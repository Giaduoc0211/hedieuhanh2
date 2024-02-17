using MalbersAnimations.Controller;
using MalbersAnimations.Scriptables;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MalbersAnimations
{
    [AddComponentMenu("Malbers/Animal Controller/Mode Align")]
    public class MModeAlign : MonoBehaviour
    {
        [RequiredField] public MAnimal animal;

        [ContextMenuItem("Attack Mode", "AddAttackMode")]
        public List<ModeID> modes = new();


        [Tooltip("If the Target animal is on any of these states then ignore alignment.")]
        public List<StateID> ignoreStates = new();

        //[Tooltip("It will search any gameobject that is a Animals on the Radius. ")]
        //public bool Animals = true;
        public LayerReference Layer = new(0);
        [Tooltip("Radius used for the Search")]
        [Min(0)] public float SearchRadius = 2f;
        [Tooltip("Radius used push closer/farther the Target when playing the Mode"), UnityEngine.Serialization.FormerlySerializedAs("DistanceRadius")]
        [Min(0)] public float Distance = 0;
        //[Tooltip("Multiplier To apply to AITarget found. Set this to Zero to ignore IAI Targets")]
        //[Min(0)] public float TargetMultiplier = 1;
        [Tooltip("Time needed to complete the Position aligment")]
        [Min(0)] public float AlignTime = 0.3f;
        [Tooltip("Time needed to complete the Rotation aligment")]
        [Min(0)] public float LookAtTime = 0.15f;


        [Tooltip("Front Offset of the Animal")]
        [Min(0)] public float FrontOffet = 0.15f;

        [Tooltip("Align Curve")]
        public AnimationCurve AlignCurve = new(new Keyframe[] { new Keyframe(0, 0), new Keyframe(1, 1) });

        public Color debugColor = new(1, 0.5f, 0, 0.2f);



        public bool debug;

        void Awake()
        {
            if (animal == null)
                animal = this.FindComponent<MAnimal>();

            if (modes == null || modes.Count == 0)
            {
                Debug.LogWarning("Please Add Modes to the Mode Align. ", this);
                enabled = false;
            }

            ignoreStates ??= new();
        }

        void OnEnable()
        { animal.OnModeStart.AddListener(StartingMode); }

        void OnDisable()
        { animal.OnModeStart.RemoveListener(StartingMode); }

        void StartingMode(int ModeID, int ability)
        {
            if (!isActiveAndEnabled) return;

            if (modes == null || modes.Count == 0 || modes.FirstOrDefault(x => x.ID == ModeID))
            {
                Align();
            }
        }

        public void Align()
        {
            //Search first Animals ... if did not find anyone then search for colliders
            if (!FindAnimals())
            {
                AlignCollider();
            }
        }

        //Store the closest Animal
        private MAnimal ClosestAnimal = null;

        private bool FindAnimals()
        {
            ClosestAnimal = null;
            float ClosestDistance = float.MaxValue;
            var Origin = transform.position;
            var radius = SearchRadius * animal.ScaleFactor;
            MDebug.DrawWireSphere(Origin, Color.red, radius, 1f);

            foreach (var a in MAnimal.Animals)
            {
                if (a == animal                                                 //We are the same animal
                    || a.Sleep                                                  //The Animal is sleep (Not working)
                    || !a.enabled                                               //The Animal Component is disabled    
                    || !MTools.Layer_in_LayerMask(a.gameObject.layer, Layer)    //Is not using the correct layer
                    || !a.gameObject.activeInHierarchy
                    || ignoreStates.Contains(a.ActiveStateID)       //Playing a skip State
                    )
                    continue; //Don't Find yourself or don't find death animals

                if (animal.transform.SameHierarchy(a.transform)) continue;      //Meaning that the animal is mounting the other animal.

                var TargetPos = a.Center;

                var dist = Vector3.Distance(Origin, TargetPos);

                Debug.DrawRay(Origin, TargetPos - Origin, Color.red, 2f);

                if (radius >= dist && ClosestDistance >= dist)
                {
                    ClosestDistance = dist;
                    ClosestAnimal = a;
                }
            }

            if (ClosestAnimal == animal) ClosestAnimal = null; //Clear the Closest animal is it the Animal owner

            //  Debug.Log($"closest {ClosestAnimal} ");

            if (ClosestAnimal)
            {
                if (!GetClosestAITarget(ClosestAnimal.transform))
                    Debuging($"Alinging to [{ClosestAnimal.name}]", this);

                return true;
            }
            return false;
        }

        private void AlignCollider()
        {
            var pos = animal.Center;

            Collider[] AllColliders = Physics.OverlapSphere(pos, SearchRadius * animal.ScaleFactor, Layer.Value, QueryTriggerInteraction.Ignore);

            Collider ClosestCollider = null;

            float ClosestDistance = float.MaxValue;

            foreach (var col in AllColliders)
            {
                if ((col.transform.SameHierarchy(animal.transform)  //Don't Find yourself
                    || !col.gameObject.activeInHierarchy            //Don't Find invisible colliders
                    || col.gameObject.isStatic                      //Don't Find Static colliders
                    || col.GetComponentInParent<MAnimal>()          //we already searched for Animals
                    || !col.enabled))       //Don't look  disabled colliders
                    continue;

                var DistCol = Vector3.Distance(transform.position, col.transform.position);

                if (ClosestDistance > DistCol)
                {
                    ClosestDistance = DistCol;
                    ClosestCollider = col;
                }
            }

            if (ClosestCollider)
            {
                if (!GetClosestAITarget(ClosestCollider.transform))
                    Debuging($"[{name}], Alinging to [{ClosestCollider.name}]", this);
            }
        }

        private bool GetClosestAITarget(Transform target)
        {
            var Center = target.position;
            var Origin = animal.Position;
            var radius = SearchRadius * animal.ScaleFactor;

            var Core = target.GetComponentInParent<IObjectCore>();
            if (Core != null) { target = Core.transform; } //Find the Core Transform

            Debug.Log($"Target: {target.name}");

            var AllAITargets = target.GetComponentsInChildren<IAITarget>();

            IAITarget ClosestAI = null;
            var ClosestDistance = float.MaxValue;

            if (AllAITargets != null)
            {
                if (AllAITargets.Length == 1)
                {
                    ClosestAI = AllAITargets[0];
                    Center = ClosestAI.GetCenterPosition();
                }
                else
                {
                    foreach (var a in AllAITargets)
                    {
                        if (!a.transform.gameObject.activeInHierarchy) continue; //Do not search Disabled AI Targets

                        var dist = Vector3.Distance(Origin, a.GetCenterPosition());

                        if (radius >= dist && ClosestDistance >= dist)
                        {
                            ClosestDistance = dist;
                            Center = a.GetCenterPosition();
                            ClosestAI = a;
                        }
                    }
                }
            }

            StartAligning(Center, ClosestAI);

            return ClosestAI != null;
        }

        private void StartAligning(Vector3 TargetCenter, IAITarget isAI)
        {
            TargetCenter.y = animal.transform.position.y;

            var raduis = Distance * animal.ScaleFactor;

            if (isAI != null)
            {
                raduis = isAI.StopDistance();// * TargetMultiplier;
                TargetCenter = isAI.GetCenterPosition();
                Debuging($" Alinging <B>AI Target</B> [{isAI.transform.name}]", isAI.transform);
            }



            if (debug)
            {
                var t = 1f;
                Debug.DrawLine(transform.position, TargetCenter, Color.white, t);
                MDebug.DrawWireSphere(TargetCenter, Color.white, 0.1f, t);
                MDebug.DrawCircle(TargetCenter, Quaternion.identity, raduis, Color.white, t);
                Debug.DrawRay(  TargetCenter, Vector3.up, Color.white, t);
            }

            StartCoroutine(MTools.AlignLookAtTransform(animal.transform, TargetCenter, LookAtTime, AlignCurve));

            //Align Look At the Zone
            if (raduis > 0)
            {
                raduis += FrontOffet*animal.ScaleFactor; //Align from the Position of the Aligner
                StartCoroutine(MTools.AlignTransformRadius(animal.transform, TargetCenter, AlignTime, raduis, AlignCurve));
            }
        }

         
        private void Debuging(string deb, Object ob)
        {
            if (debug) Debug.Log($"<B>[{animal.name}]</B> {deb}", ob);
        }



#if UNITY_EDITOR

        [ContextMenu("Add Attack Mode")]
        private void AddAttackMode()
        {
            Reset();
        }


        void Reset()
        {

            animal = gameObject.FindComponent<MAnimal>();

            modes = new List<ModeID>
            {
                MTools.GetResource<ModeID>("Attack1")
            };

            ignoreStates = new List<StateID>
            {
                MTools.GetResource<StateID>("Death")
            };


            MTools.SetDirty(this);
        }


        void OnDrawGizmosSelected()
        {
            if (animal && debug)
            {
                var scale = animal.ScaleFactor;
                var Pos = animal.Position + (FrontOffet * scale * animal.Forward);

                Handles.color = debugColor;
                var c = debugColor; c.a = 1;
                Handles.color = c;
                Handles.DrawWireDisc(Pos, Vector3.up, SearchRadius * scale);

                Handles.color = (c + Color.white) / 2;
                Handles.DrawWireDisc(Pos, Vector3.up, Distance * scale);


                Gizmos.color = debugColor;
                Gizmos.DrawSphere(Pos, 0.1f * scale);
                Gizmos.color = c;
                Gizmos.DrawWireSphere(Pos, 0.1f * scale);
            }
        }
#endif
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(MModeAlign)), CanEditMultipleObjects]
    public class MModeAlignEditor : Editor
    {
        SerializedProperty animal,
            modes, ignoreStates,// AnimalsOnly,
            Layer, debug, LookRadius, DistanceRadius, AlignTime, LookAtTime, FrontOffet, //TargetMultiplier, 
            AlignCurve, debugColor/*, pushTarget*/;
        private void OnEnable()
        {
            animal = serializedObject.FindProperty("animal");
            modes = serializedObject.FindProperty("modes");
            ignoreStates = serializedObject.FindProperty("ignoreStates");
            // AnimalsOnly = serializedObject.FindProperty("Animals");
            Layer = serializedObject.FindProperty("Layer");
            LookRadius = serializedObject.FindProperty("SearchRadius");
            DistanceRadius = serializedObject.FindProperty("Distance");
            AlignTime = serializedObject.FindProperty("AlignTime");
            LookAtTime = serializedObject.FindProperty("LookAtTime");
            debugColor = serializedObject.FindProperty("debugColor");
            //TargetMultiplier = serializedObject.FindProperty("TargetMultiplier");
            FrontOffet = serializedObject.FindProperty("FrontOffet");
            debug = serializedObject.FindProperty("debug");
            AlignCurve = serializedObject.FindProperty("AlignCurve");

        }

        private GUIContent _ReactIcon;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            MalbersEditor.DrawDescription($"Execute a LookAt towards the closest Animal or GameObject  when is playing a Mode on the list");

            using (new GUILayout.VerticalScope(EditorStyles.helpBox)) 
            {
                using (new GUILayout.HorizontalScope())
                { 
                    if (Application.isPlaying)
                    {

                        if (_ReactIcon == null)
                        {
                            _ReactIcon = EditorGUIUtility.IconContent("d_PlayButton@2x");
                            _ReactIcon.tooltip = "Play at Runtime";
                        }

                        if (GUILayout.Button(_ReactIcon, EditorStyles.miniButton, GUILayout.Width(28f), GUILayout.Height(20)))
                        {
                            (target as MModeAlign).Align();
                        }
                    }


                    EditorGUILayout.PropertyField(animal);
                    if (debug.boolValue)
                        EditorGUILayout.PropertyField(debugColor, GUIContent.none, GUILayout.Width(36));

                    MalbersEditor.DrawDebugIcon(debug);
                }

                EditorGUILayout.PropertyField(Layer);

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(modes, true);
                EditorGUILayout.PropertyField(ignoreStates, true);
                EditorGUI.indentLevel--;
                //  EditorGUILayout.PropertyField(AnimalsOnly);
            }


            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(LookRadius);
                if (LookRadius.floatValue > 0)
                    EditorGUILayout.PropertyField(LookAtTime);
            }

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(DistanceRadius);

                if (DistanceRadius.floatValue > 0)
                {
                    EditorGUILayout.PropertyField(AlignTime);
                    EditorGUILayout.PropertyField(FrontOffet);
                }
               // EditorGUILayout.PropertyField(TargetMultiplier);
                EditorGUILayout.PropertyField(AlignCurve);

            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif

}