using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Shared base for parameterizers that extract combinatorial test data from
    /// a single NUnit <see cref="IParameterDataSource"/> attribute type.
    /// </summary>
    abstract class DataSourceAttributeParameterizer<TAttr> : Parameterizer
        where TAttr : System.Attribute, IParameterDataSource
    {
        internal override bool CanParameterize(IMethodInfo method)
        {
            return GetParametersWithAttribute<TAttr>(method).Count > 0;
        }

        protected override HashSet<TestCaseData> Parameterize(TestCaseData originalTestCase, IMethodInfo method)
        {
            var arguments = new List<List<object>>();

            foreach (var parameter in GetParametersWithAttribute<TAttr>(method))
            {
                var attrs = parameter.GetCustomAttributes<TAttr>(false);
                var data = attrs?[0].GetData(parameter);
                var list = new List<object>();
                if (data != null)
                {
                    foreach (var item in data)
                        list.Add(item);
                }
                arguments.Add(list);
            }

            return GenerateCombinatorialParametricTestCases(arguments, originalTestCase);
        }
    }
}
