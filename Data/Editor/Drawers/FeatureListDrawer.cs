using System;
using System.Reflection;
using CupkekGames.EditorUI;
using UnityEditor;

namespace CupkekGames.Data.Editor
{
    /// <summary>
    /// Property drawer for <see cref="IFeature"/> entries in
    /// <c>[SerializeReference] List&lt;IFeature&gt;</c> (and the narrower
    /// <c>List&lt;IItemFeature&gt;</c>, <c>List&lt;IUnitFeatureDefinition&gt;</c>
    /// variants). The shared <see cref="PolymorphicReferenceDrawer{TBase}"/>
    /// in editorui does the heavy lifting; this subclass just reads
    /// <see cref="FeatureGroupAttribute"/> so authoring sees grouped
    /// categories in the dropdown.
    /// </summary>
    [CustomPropertyDrawer(typeof(IFeature), useForChildren: true)]
    public class FeatureDrawer : PolymorphicReferenceDrawer<IFeature>
    {
        protected override string GetGroup(Type type)
        {
            return type.GetCustomAttribute<FeatureGroupAttribute>()?.Group;
        }
    }
}
