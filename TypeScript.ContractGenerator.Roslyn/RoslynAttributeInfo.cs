using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

using SkbKontur.TypeScript.ContractGenerator.Abstractions;

namespace SkbKontur.TypeScript.ContractGenerator.Roslyn
{
    public class RoslynAttributeInfo : IAttributeInfo
    {
        public RoslynAttributeInfo(AttributeData attributeData)
        {
            if (attributeData.AttributeClass == null)
                throw new NotSupportedException();

            Attribute = attributeData;
            AttributeType = RoslynTypeInfo.From(attributeData.AttributeClass);
            AttributeData = attributeData.NamedArguments
                                         .Concat((attributeData.AttributeConstructor?.Parameters ?? (IEnumerable<IParameterSymbol>)new IParameterSymbol[0])
                                                     .Select(x => x.Name)
                                                     .Zip(attributeData.ConstructorArguments, (name, value) => new KeyValuePair<string, TypedConstant>(name, value)))
                                         .ToDictionary(x => x.Key, x => GetValue(x.Value));
        }

        public AttributeData Attribute { get; }

        public ITypeInfo AttributeType { get; }
        public Dictionary<string, object?> AttributeData { get; }

        private static object? GetValue(TypedConstant argument)
        {
            if (argument.Kind == TypedConstantKind.Array)
                return argument.Values.Select(GetValue).ToArray();
            if (argument.Value is ITypeSymbol typeSymbol)
                return RoslynTypeInfo.From(typeSymbol);
            return argument.Value;
        }
    }
}