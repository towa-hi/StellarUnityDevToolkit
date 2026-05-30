using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PunchEffect))]
public class PunchEffectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("Preview Punch"))
            {
                foreach (Object selectedTarget in targets)
                {
                    PunchEffect punchEffect = (PunchEffect)selectedTarget;
                    punchEffect.Punch();
                }
            }
        }

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to preview the DOTween punch animation.", MessageType.Info);
        }
    }
}
