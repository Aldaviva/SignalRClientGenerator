using Microsoft.CodeAnalysis;

namespace SignalRClientGenerator;

internal record ClassModel(string name, string ns, Accessibility visibility, EquatableList<InterfaceModel> incomingInterfaces, EquatableList<InterfaceModel> outgoingInterfaces);

internal record InterfaceModel(string fullyQualifiedName, EquatableList<MethodModel> methods);

internal record MethodModel(string name, string fqReturnType, EquatableList<MethodParameterModel> parameters);

internal record MethodParameterModel(string fqType, string name, bool nullable, string? defaultValue, bool varArg);