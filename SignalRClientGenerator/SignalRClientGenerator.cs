using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Unfucked;

namespace SignalRClientGenerator;

/// <summary>
/// Based on <see href="https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.cookbook.md#auto-interface-implementation"/>
/// </summary>
[Generator(LanguageNames.CSharp)]
public class SignalRClientGenerator: IIncrementalGenerator {

    private const string GENERATED_NAMESPACE = "SignalRClientGenerator";
    private const string GENERATOR_NAME      = nameof(SignalRClientGenerator);

    private static readonly string GENERATOR_VERSION = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        context.RegisterPostInitializationOutput(ctx => {
            ctx.AddEmbeddedAttributeDefinition();
            ctx.AddSource("GenerateSignalRClientAttribute.g.cs", SourceText.From(
                $$"""
                using System;
                using Microsoft.CodeAnalysis;

                namespace {{GENERATED_NAMESPACE}};

                /// <summary>
                /// <para>To autogenerate a strongly-typed SignalR client, add this attribute to a partial class. Pass the interfaces which represent the events sent to and from the client.</para>
                /// <para>Example:</para>
                /// <para><code>[GenerateSignalRClient(Incoming = [typeof(EventsToClient)], Outgoing = [typeof(EventsToServer)])]
                /// public partial class SampleClient;</code></para>
                /// </summary>
                [AttributeUsage(AttributeTargets.Class, Inherited=false, AllowMultiple=false)]
                [Embedded]
                [System.Diagnostics.DebuggerNonUserCode, System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage, System.CodeDom.Compiler.GeneratedCode("{{GENERATOR_NAME}}", "{{GENERATOR_VERSION}}")]
                internal sealed class GenerateSignalRClientAttribute(): Attribute {

                    /// <summary>Zero or more interfaces of SignalR messages sent to the client.</summary>
                    public required Type[] Incoming { get; init; }
                    
                    /// <summary>Zero or more interfaces of SignalR messages sent from the client.</summary>
                    public Type[] Outgoing { get; init; } = [];

                }
                """, Encoding.UTF8));
        });

        IncrementalValuesProvider<ClassModel> provider = context.SyntaxProvider.ForAttributeWithMetadataName($"{GENERATED_NAMESPACE}.GenerateSignalRClientAttribute",
            (node, ct) => node is ClassDeclarationSyntax,
            (syntaxContext, ct) => new ClassModel(syntaxContext.TargetSymbol.Name, syntaxContext.TargetSymbol.ContainingNamespace.ToDisplayString(), syntaxContext.TargetSymbol.DeclaredAccessibility,
                getInterfaceModels(syntaxContext.Attributes[0], true), getInterfaceModels(syntaxContext.Attributes[0], false)));

        context.RegisterSourceOutput(provider, static (ctx, classModel) => {
            StringBuilder builder = new();

            StringBuilder interfaceBuilder =
                new($$"""
                    /// <summary>Interface for <see cref="{{classModel.name}}" />.</summary>
                    [System.CodeDom.Compiler.GeneratedCode("{{GENERATOR_NAME}}", "{{GENERATOR_VERSION}}")]
                    public interface I{{classModel.name}}{{(classModel.outgoingInterfaces.Any() ? ":" : "")}} {{classModel.outgoingInterfaces.Select(i => i.fullyQualifiedName).Join(", ")}} {


                    """);

            string classVisibility = classModel.visibility switch {
                Accessibility.Public               => "public",
                Accessibility.Internal             => "internal",
                Accessibility.Protected            => "protected",
                Accessibility.ProtectedOrInternal  => "protected internal",
                Accessibility.ProtectedAndInternal => "private protected",
                Accessibility.Private              => "private",
                _                                  => string.Empty
            };

            builder.AppendLine(
                $$"""
                #nullable enable

                namespace {{classModel.ns}};

                {{classVisibility}} partial class {{classModel.name}}: I{{classModel.name}} {

                    /// <inheritdoc cref="I{{classModel.name}}.HubConnection" />
                    [System.Diagnostics.DebuggerNonUserCode, System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage, System.CodeDom.Compiler.GeneratedCode("{{GENERATOR_NAME}}", "{{GENERATOR_VERSION}}")]
                    public Microsoft.AspNetCore.SignalR.Client.HubConnection HubConnection { get; }
                    
                """);

            interfaceBuilder.AppendLine("    /// <summary>Sends and receives SignalR messages.</summary>")
                .AppendLine("    public Microsoft.AspNetCore.SignalR.Client.HubConnection HubConnection { get; }")
                .AppendLine();

            foreach (InterfaceModel outgoingInterface in classModel.outgoingInterfaces) {
                foreach (MethodModel method in outgoingInterface.methods) {
                    StringBuilder outgoingMethodSignatureBuilder = new();
                    outgoingMethodSignatureBuilder.Append($"{method.fqReturnType} {method.name}(");
                    bool firstParam = true;
                    foreach (MethodParameterModel methodParam in method.parameters) {
                        if (firstParam) {
                            firstParam = false;
                        } else {
                            outgoingMethodSignatureBuilder.Append(", ");
                        }
                        outgoingMethodSignatureBuilder.Append(methodParam.varArg ? "params " : "")
                            .Append(methodParam.fqType)
                            .Append(methodParam.nullable ? "? " : " ")
                            .Append(methodParam.name)
                            .Append(methodParam.defaultValue is {} def ? " = " + def : "");
                    }

                    interfaceBuilder.Append("    /// <inheritdoc cref=\"")
                        .Append(outgoingInterface.fullyQualifiedName)
                        .Append('.')
                        .Append(method.name)
                        .AppendLine("\" />")
                        .AppendLine("    /// <param name=\"cancellationToken\">Cancel sending message from client</param>")
                        .Append("    ")
                        .Append(outgoingMethodSignatureBuilder)
                        .Append(method.parameters.Any() ? ", " : "")
                        .AppendLine("System.Threading.CancellationToken cancellationToken);\n");

                    builder.Append("    /// <inheritdoc cref=\"")
                        .Append(outgoingInterface.fullyQualifiedName)
                        .Append('.')
                        .Append(method.name)
                        .AppendLine("\" />")
                        .AppendLine(
                            $"    [System.Diagnostics.DebuggerNonUserCode, System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage, System.CodeDom.Compiler.GeneratedCode(\"{GENERATOR_NAME}\", \"{GENERATOR_VERSION}\")]")
                        .Append("    public ")
                        .Append(outgoingMethodSignatureBuilder)
                        .Append(") => ")
                        .Append(method.name)
                        .Append('(');
                    foreach (MethodParameterModel methodParam in method.parameters) {
                        builder.Append(methodParam.name).Append(", ");
                    }
                    builder.AppendLine("System.Threading.CancellationToken.None);\n");

                    builder.AppendLine("    /// <inheritdoc />")
                        .AppendLine(
                            $"    [System.Diagnostics.DebuggerNonUserCode, System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage, System.CodeDom.Compiler.GeneratedCode(\"{GENERATOR_NAME}\", \"{GENERATOR_VERSION}\")]")
                        .Append("    public async ")
                        .Append(outgoingMethodSignatureBuilder)
                        .Append(method.parameters.Any() ? ", " : "")
                        .AppendLine("System.Threading.CancellationToken cancellationToken) =>")
                        .Append("        await Microsoft.AspNetCore.SignalR.Client.HubConnectionExtensions.InvokeAsync(HubConnection, \"")
                        .Append(method.name)
                        .Append("\", ");
                    foreach (MethodParameterModel methodParam in method.parameters) {
                        builder.Append(methodParam.name).Append(", ");
                    }
                    builder.AppendLine("cancellationToken);\n");
                }
            }

            StringBuilder onSetHubValueBuilder = new();

            foreach (InterfaceModel incomingInterface in classModel.incomingInterfaces) {
                foreach (MethodModel method in incomingInterface.methods) {
                    string eventType = $"{method.name.ToUpperFirstLetter()}Handler";
                    interfaceBuilder.AppendLine("    /// <param name=\"sender\">SignalR client</param>")
                        .Append("    /// <inheritdoc cref=\"")
                        .Append(incomingInterface.fullyQualifiedName)
                        .Append('.')
                        .Append(method.name)
                        .AppendLine("\" />")
                        .Append("    delegate ")
                        .Append(method.fqReturnType)
                        .Append(' ')
                        .Append(eventType)
                        .Append("(I")
                        .Append(classModel.name)
                        .Append(" sender");
                    foreach (MethodParameterModel methodParam in method.parameters) {
                        interfaceBuilder.Append(", ")
                            .Append(methodParam.varArg ? "params" : "")
                            .Append(methodParam.fqType)
                            .Append(methodParam.nullable ? "? " : " ")
                            .Append(methodParam.name);
                    }

                    interfaceBuilder.AppendLine(");")
                        .AppendLine()
                        .Append("    /// <inheritdoc cref=\"")
                        .Append(incomingInterface.fullyQualifiedName)
                        .Append('.')
                        .Append(method.name)
                        .AppendLine("\" path=\"/*[(self::summary or self::remarks or self::seealso)]\" />")
                        .AppendLine($"    event {eventType}? {method.name};\n");

                    builder.Append("    /// <inheritdoc cref=\"I")
                        .Append(classModel.name)
                        .Append('.')
                        .Append(method.name)
                        .AppendLine("\" />")
                        .AppendLine($"    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage, System.CodeDom.Compiler.GeneratedCode(\"{GENERATOR_NAME}\", \"{GENERATOR_VERSION}\")]")
                        .Append("    public event I")
                        .Append(classModel.name)
                        .Append('.')
                        .Append(eventType)
                        .Append("? ")
                        .Append(method.name)
                        .AppendLine(";\n");

                    onSetHubValueBuilder.Append("        Microsoft.AspNetCore.SignalR.Client.HubConnectionExtensions.On")
                        .Append(method.parameters.Any() ? "<" : "");
                    bool firstParam = true;
                    foreach (MethodParameterModel methodParam in method.parameters) {
                        if (firstParam) {
                            firstParam = false;
                        } else {
                            onSetHubValueBuilder.Append(", ");
                        }
                        onSetHubValueBuilder.Append(methodParam.fqType)
                            .Append(methodParam.nullable ? "?" : "");
                    }

                    onSetHubValueBuilder.Append(method.parameters.Any() ? ">" : "")
                        .Append($"(HubConnection, \"{method.name}\", async (");
                    firstParam = true;
                    foreach (MethodParameterModel methodParam in method.parameters) {
                        if (firstParam) {
                            firstParam = false;
                        } else {
                            onSetHubValueBuilder.Append(", ");
                        }
                        onSetHubValueBuilder.Append(methodParam.fqType).Append(' ').Append(methodParam.name);
                    }
                    onSetHubValueBuilder.Append(") => await (").Append(method.name).Append("?.Invoke(this");
                    foreach (MethodParameterModel methodParam in method.parameters) {
                        onSetHubValueBuilder.Append(", ").Append(methodParam.name);
                    }
                    onSetHubValueBuilder.AppendLine(") ?? System.Threading.Tasks.Task.CompletedTask));");
                }
            }
            builder.AppendLine("    /// <summary>Creates a strongly-typed SignalR client for a given <paramref name=\"hubConnection\"/>.</summary>")
                .AppendLine("    /// <param name=\"hubConnection\">Sends and receives SignalR messages. Created by <see cref=\"Microsoft.AspNetCore.SignalR.Client.HubConnectionBuilder\"/>.</param>")
                .AppendLine(
                    $"    [System.Diagnostics.DebuggerNonUserCode, System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage, System.CodeDom.Compiler.GeneratedCode(\"{GENERATOR_NAME}\", \"{GENERATOR_VERSION}\")]")
                .AppendLine($"    public {classModel.name}(Microsoft.AspNetCore.SignalR.Client.HubConnection hubConnection) {{")
                .AppendLine("        HubConnection = hubConnection;")
                .Append(onSetHubValueBuilder)
                .AppendLine("    }");

            builder.AppendLine("}\n");

            builder.Append(interfaceBuilder).AppendLine("}");

            ctx.AddSource($"{classModel.name}.g.cs", builder.ToString().Replace("\r\n", "\n"));
        });
    }

    private static EquatableList<InterfaceModel> getInterfaceModels(AttributeData attribute, bool isIncoming) {
        EquatableList<InterfaceModel> interfaces = [];

        if (attribute.NamedArguments.FirstOrNull(pair => pair.Key == (isIncoming ? "Incoming" : "Outgoing"))?.Value is { IsNull: false } attributePropertyInitializer) {
            IEnumerable<INamedTypeSymbol> interfaceReferences = attributePropertyInitializer.Values
                .Select(v => v.Value)
                .OfType<INamedTypeSymbol>()
                .Where(arg => arg.TypeKind == TypeKind.Interface)
                .SelectMany(i => i.AllInterfaces.Insert(0, i));

            foreach (INamedTypeSymbol interfaceReference in interfaceReferences) {
                EquatableList<MethodModel> interfaceMethods = [];
                foreach (IMethodSymbol interfaceMethod in interfaceReference.GetMembers().OfType<IMethodSymbol>()) {

                    EquatableList<MethodParameterModel> methodParams = [];
                    foreach (IParameterSymbol methodParam in interfaceMethod.Parameters) {
                        methodParams.Add(new MethodParameterModel(methodParam.Type.ToDisplayString(), methodParam.Name, methodParam.IsOptional,
                            methodParam.HasExplicitDefaultValue ? methodParam.ExplicitDefaultValue?.ToString() ?? "default" : null, methodParam.IsParams));
                    }

                    interfaceMethods.Add(new MethodModel(interfaceMethod.Name, interfaceMethod.ReturnsVoid ? "void" : interfaceMethod.ReturnType.ToDisplayString(), methodParams));
                }

                interfaces.Add(new InterfaceModel(interfaceReference.ToDisplayString(), interfaceMethods));
            }
        }
        return interfaces;
    }

}