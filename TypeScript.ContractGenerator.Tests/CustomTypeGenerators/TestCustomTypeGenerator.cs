using System;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using SkbKontur.TypeScript.ContractGenerator.Abstractions;
using SkbKontur.TypeScript.ContractGenerator.CodeDom;
using SkbKontur.TypeScript.ContractGenerator.Extensions;
using SkbKontur.TypeScript.ContractGenerator.Internals;
using SkbKontur.TypeScript.ContractGenerator.Roslyn;
using SkbKontur.TypeScript.ContractGenerator.Tests.Types;
using SkbKontur.TypeScript.ContractGenerator.TypeBuilders;

using TypeInfo = SkbKontur.TypeScript.ContractGenerator.Internals.TypeInfo;

namespace SkbKontur.TypeScript.ContractGenerator.Tests.CustomTypeGenerators
{
    public class TestCustomTypeGenerator : ICustomTypeGenerator
    {
        public string GetTypeLocation(ITypeInfo type)
        {
            return "";
        }

        public ITypeBuildingContext? ResolveType(string initialUnitPath, ITypeGenerator typeGenerator, ITypeInfo typeInfo, ITypeScriptUnitFactory unitFactory)
        {
            if (typeInfo.Equals(TypeInfo.From<MethodRootType>()) || typeInfo.Equals(TypeInfo.From<NullableReferenceMethodType>()))
                return new MethodTypeBuildingContext(unitFactory.GetOrCreateTypeUnit(initialUnitPath), typeInfo);

            if (CollectionTypeBuildingContext.Accept(typeInfo))
                return new CollectionTypeBuildingContext(typeInfo);

            if (typeInfo.Equals(TypeInfo.From<TimeSpan>()))
                return new StringBuildingContext(typeInfo);

            if (typeInfo.IsAbstract)
                return new AbstractTypeBuildingContext(unitFactory.GetOrCreateTypeUnit(initialUnitPath), typeInfo);

            return null;
        }

        public TypeScriptTypeMemberDeclaration? ResolveProperty(TypeScriptUnit unit, ITypeGenerator typeGenerator, ITypeInfo typeInfo, IPropertyInfo propertyInfo)
        {
            if (!TryGetGetOnlyEnumPropertyValue(typeInfo, propertyInfo, out var value))
                return null;

            return new TypeScriptTypeMemberDeclaration
                {
                    Name = propertyInfo.Name.ToLowerCamelCase(),
                    Optional = false,
                    Type = GetConstEnumType(typeGenerator, unit, propertyInfo, value!),
                };
        }

        private static TypeScriptType GetConstEnumType(ITypeGenerator typeGenerator, TypeScriptUnit unit, IPropertyInfo property, string value)
        {
            return new TypeScriptEnumValueType(typeGenerator.BuildAndImportType(unit, property.PropertyType), value);
        }

        private static bool TryGetGetOnlyEnumPropertyValue(ITypeInfo typeInfo, IPropertyInfo propertyInfo, out string? value)
        {
            value = typeInfo is TypeInfo
                        ? GetValueFromPropertyInfo(typeInfo, propertyInfo)
                        : GetValueFromPropertySymbol(propertyInfo);
            return !string.IsNullOrEmpty(value);
        }

        private static string? GetValueFromPropertyInfo(ITypeInfo typeInfo, IPropertyInfo propertyInfo)
        {
            var property = ((PropertyWrapper)propertyInfo).Property;
            var type = ((TypeInfo)typeInfo).Type;
            var hasDefaultConstructor = type.GetConstructors().Any(x => x.GetParameters().Length == 0);
            var hasInferAttribute = property.GetCustomAttributes<InferValueAttribute>(true).Any();
            if (!property.PropertyType.IsEnum || property.CanWrite || !hasDefaultConstructor || !hasInferAttribute)
                return null;
            return property.GetMethod?.Invoke(Activator.CreateInstance(type), null)?.ToString();
        }

        private static string? GetValueFromPropertySymbol(IPropertyInfo propertyInfo)
        {
            var property = ((RoslynPropertyInfo)propertyInfo).PropertySymbol;
            var hasInferAttribute = propertyInfo.GetAttributes(TypeInfo.From<InferValueAttribute>()).Any();
            if (!hasInferAttribute || !propertyInfo.PropertyType.IsEnum || property.SetMethod != null)
                return null;

            var syntaxNode = property.GetMethod?.DeclaringSyntaxReferences.Single().GetSyntax();
            if (syntaxNode is ArrowExpressionClauseSyntax arrowExpression && arrowExpression.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                if (memberAccess.Expression is IdentifierNameSyntax identifier && identifier.Identifier.Text == propertyInfo.PropertyType.Name)
                    return memberAccess.Name.Identifier.ToString();
            }

            return null;
        }
    }
}