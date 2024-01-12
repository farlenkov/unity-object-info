using UnityEditor;

namespace UnityScriptTemplate_Generated
{
	public static partial class ScriptCreateMenu_638406844748605004
	{
		public static void CreateScript(string templatePath, string scriptName)
		{
			ProjectWindowUtil.CreateScriptAssetFromTemplateFile(
				templatePath,
				scriptName);
		}

		[MenuItem("Assets/Create/Object Info/ComponentInfo.cs", priority = 80)]
		public static void Create_638406844748605005()
		{
			CreateScript(
				"Packages/com.farlenkov.unity-object-info/Editor/ScriptTemplates/Templates/ComponentInfo.cs.txt",
				"NewComponentInfo.cs");
		}

		[MenuItem("Assets/Create/Object Info/EntityInfo.cs", priority = 80)]
		public static void Create_638406844748605006()
		{
			CreateScript(
				"Packages/com.farlenkov.unity-object-info/Editor/ScriptTemplates/Templates/EntityInfo.cs.txt",
				"NewEntityInfo.cs");
		}
	}
}
