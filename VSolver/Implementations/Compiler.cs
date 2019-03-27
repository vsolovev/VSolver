using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using VSolver.Interfaces;

namespace VSolver.Implementations
{
    public class Compiler : ICompiler
    {
        public CreateInstanceFunction Compile(Type t, ConstructorInfo constructorInfo, CreateInstanceFunction[] constructorDependencies, IDictionary<PropertyInfo, CreateInstanceFunction> properties)
        {
            var constructorParameters = constructorInfo.GetParameters();
            var cDepList = new Expression[constructorDependencies.Length];
            for (var i = 0; i < constructorDependencies.Length; i++)
            {
                cDepList[i] = CreateConvertExpression(constructorDependencies[i], constructorParameters[i].ParameterType);
            }

            var constructorCallExp = Expression.New(
                constructor: constructorInfo, 
                arguments: cDepList);

            var pDepList = properties.Select(
                propertyItem => Expression.Bind(
                    member: propertyItem.Key,
                    expression: CreateConvertExpression(propertyItem.Value, propertyItem.Key.PropertyType)))
                .Cast<MemberBinding>()
                .ToList();

            var finalExpression = Expression.MemberInit(constructorCallExp, pDepList);

            var lambdaExpression = Expression.Lambda(
                delegateType: typeof(CreateInstanceFunction), 
                body: finalExpression);
           
            var compiledExpression = lambdaExpression.Compile();
            return (CreateInstanceFunction) compiledExpression;
        }

        private UnaryExpression CreateConvertExpression(CreateInstanceFunction func, Type type)
        {
            return Expression.Convert(Expression.Invoke(Expression.Constant(func)), type);
        }
    }
}