using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using ZT = ZToolsKtane;
using Rnd = UnityEngine.Random;
using Math = ExMath;
using UnityEditor.UI;
using JetBrains.Annotations;
using UnityEditor;
using static MolePatrol;

public class MolePatrol : MonoBehaviour
{

    public KMBombInfo Bomb;
    public KMAudio Audio;

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    List<string> AudioOutNames = new List<string> { "Out1", "Out2", "Out3"};
    bool isSequencePlaying = false;
    bool isSequenceHasPlayed = false;
    bool isSwapping = false;
    bool isMultipleMoving = false;
    List<int> currentlySelectedMoleByIndex = new List<int>();
    public KMSelectable[] Buttons;
    List<Mole> MoleList = new List<Mole>();
    List<int> MoleSequence;
    public List<GameObject> MoleGObjects;
    public Material[] materials = new Material[6];
    Dictionary<Positions, MaterialPair> MoleColorDictionary;
    Phases currentPhase = Phases.Sequence1;

    public enum Positions { TL, T, TR, R, DR, D, DL, L}
    public enum ColorNames { Red, Green, Blue, Magenta, Yellow, Cyan}
    public enum Phases { Sequence1, Input1, Sequence2, Input2, Sequence3, Input3, }

    public class MaterialPair
    {
        public Material MainMat;
        public Material SecMat;

        public MaterialPair(Material mainMat, Material secMat)
        {
            MainMat = mainMat;
            SecMat = secMat;
        }
    }

    public class Mole
    {
        public Positions CurrentPosition;
        public bool IsUp;
        public bool IsLookUp;
        public bool IsHandUp;
        public ColorNames MainColor;
        public ColorNames SecColor;
        public Transform Transform;
        public Vector3 DownPosition;
        public Vector3 UpPosition;
        public Coroutine ActiveMovement;
        public MeshRenderer MeshRenderer;
        public MeshRenderer MeshrendererHandR;
        public SkinnedMeshRenderer MeshRendererHandL;
        public SkinnedMeshRenderer SMRLookUp;
        public SkinnedMeshRenderer SMRHandUp;

        public Mole(GameObject gameObject, Positions pos, MaterialPair mp)
        {
            CurrentPosition = pos;
            MeshRenderer = gameObject.GetComponent<MeshRenderer>();
            MeshRendererHandL = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>().FirstOrDefault(r => r.name.Contains("HandL"));
            MeshrendererHandR = gameObject.GetComponentsInChildren<MeshRenderer>().FirstOrDefault(r => r.name.Contains("HandR"));

            //Assign Colors
            Material[] mats = MeshRenderer.materials;
            mats[0] = mp.MainMat;
            mats[1] = mp.SecMat;
            MeshRenderer.materials = mats;
            MeshRendererHandL.material = mats[1];
            MeshrendererHandR.material = mats[1];
            string cleanName = mats[0].name.Replace(" (Instance)", "");
            MainColor = (ColorNames)Enum.Parse(typeof(ColorNames), cleanName, true);
            cleanName = mats[1].name.Replace(" (Instance)", "");
            SecColor = (ColorNames)Enum.Parse(typeof(ColorNames), cleanName, true);

            IsUp = false;
            IsLookUp = false;
            IsHandUp = false;
            foreach (var smr in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (smr.name.Contains("Face"))
                    SMRLookUp = smr;
                else if (smr.name.Contains("Hand"))
                    SMRHandUp = smr;
            }

            Transform = gameObject.transform;
            DownPosition = gameObject.transform.localPosition;
            UpPosition = gameObject.transform.localPosition + new Vector3(0, 0, 0.0015f);
        }
    }

    IEnumerator MoveMole(Mole mole, Vector3 target, float duration, bool isChangeColor = false)
    {
        Vector3 start = mole.Transform.localPosition;
        float elapsed = 0f;

        Vector3 direction = target - start;
        bool goingUp = direction.z > 0f;

        if (goingUp && isMultipleMoving == false)
        {
            Audio.PlaySoundAtTransform(AudioOutNames[Rnd.Range(0, 2)], mole.Transform);
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            mole.Transform.localPosition = Vector3.Lerp(start, target, t);

            float weight = goingUp ? Mathf.Lerp(0f, 100f, t) : Mathf.Lerp(100f, 0f, t);
            mole.SMRLookUp.SetBlendShapeWeight(0, weight);

            yield return null;
        }

        mole.Transform.localPosition = target;
        mole.SMRLookUp.SetBlendShapeWeight(0, goingUp ? 100f : 0f);
        mole.ActiveMovement = null;
    }

    IEnumerator MoleRaiseHand(Mole mole, float duration, bool isSelected = false)
    {
        Debug.Log(mole.ToString());
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float weight = isSelected ? Mathf.Lerp(0f, 100f, t) : Mathf.Lerp(100f, 0f, t);
            mole.SMRHandUp.SetBlendShapeWeight(0, weight);

            yield return null;
        }

        mole.SMRHandUp.SetBlendShapeWeight(0, isSelected ? 100f : 0f);
    }

    IEnumerator PlayMoleSequence()
    {
        foreach (int i in MoleSequence)
        {
            MolePopUpHandler(i, true);
            yield return new WaitForSeconds(.75f);
            MolePopUpHandler(i, false);
        }
        isSequenceHasPlayed = true;
        isSequencePlaying = false;
    }

    IEnumerator SwapMoleAnimation(int mole1, int mole2, Transform tempAudioPos)
    {
        Mole m1 = MoleList[mole1];
        Mole m2 = MoleList[mole2];

        StartCoroutine(MoleRaiseHand(m1, .1f));
        
        MolePopUpHandler(mole1);
        MolePopUpHandler(mole2);

        yield return new WaitForSeconds(.5f);

        var temp = m1.MeshRenderer.materials;
        m1.MeshRenderer.materials = m2.MeshRenderer.materials;
        m2.MeshRenderer.materials = temp;

        temp = m1.MeshRendererHandL.materials;
        m1.MeshRendererHandL.materials = m2.MeshRendererHandL.materials;
        m2.MeshRendererHandL.materials = temp;

        temp = m1.MeshrendererHandR.materials;
        m1.MeshrendererHandR.materials = m2.MeshrendererHandR.materials;
        m2.MeshrendererHandR.materials = temp;

        var temp1 = m1.MainColor;
        m1.MainColor = m2.MainColor;
        m2.MainColor = temp1;

        temp1 = m1.SecColor;
        m1.SecColor = m2.SecColor;
        m2.SecColor = temp1;

        if (isMultipleMoving)
        {
            Audio.PlaySoundAtTransform(AudioOutNames[Rnd.Range(0, 2)], tempAudioPos);
        }

        MolePopUpHandler(mole1, true);
        MolePopUpHandler(mole2, true);

        currentlySelectedMoleByIndex.Clear();
        isSwapping = false;
    }

    void Awake()
    { //Avoid doing calculations in here regarding edgework. Just use this for setting up buttons for simplicity.
        ModuleId = ModuleIdCounter++;
        GetComponent<KMBombModule>().OnActivate += Activate;
        foreach (KMSelectable button in Buttons)
        {
            button.OnInteract += delegate () { InputHandler(button); return false; };
            //button.OnHighlight += delegate () { MolePopUpHandler(button, true);};
            //button.OnHighlightEnded += delegate () { MolePopUpHandler(button); };
        }

    }

    void OnDestroy()
    { //Shit you need to do when the bomb ends

    }

    void Activate()
    { //Shit that should happen when the bomb arrives (factory)/Lights turn on
        
    }

    void Start()
    { //Shit that you calculate, usually a majority if not all of the module
        //Make Color Dictionary
        MoleColorDictionary = Enum.GetValues(typeof(Positions))
            .Cast<Positions>()
            .ToDictionary(pos => pos, pos => GetRandomMaterialPair());

        //Setup Moles To List
        for (int i = 0; i < MoleGObjects.Count; i++)
            MoleList.Add(new Mole(MoleGObjects[i], MoleColorDictionary.ElementAt(i).Key, MoleColorDictionary.ElementAt(i).Value));

        //Generate sequence of 5
        MoleSequence = Enumerable.Range(0, MoleList.Count).OrderBy(_ => Rnd.value).Take(5).ToList();
    }

    void Update()
    { //Shit that happens at any point after initialization
        if (ModuleSolved) return;
    }

    void InputHandler(KMSelectable button)
    {
        if (isSwapping) return;
        int index = Array.IndexOf(Buttons, button);

        if ((int)currentPhase % 2 == 0)
        {
            if (index != 8 && !isSequencePlaying)
            {
                isSequencePlaying = true;
                StartCoroutine(PlayMoleSequence());
            }
            else if (index == 8 && !isSequencePlaying && isSequenceHasPlayed)
            {
                currentPhase++;
                isMultipleMoving = true;
                Audio.PlaySoundAtTransform(AudioOutNames[Rnd.Range(0, 2)], this.transform);
                for (int i = 0; i < MoleGObjects.Count; i++) MolePopUpHandler(i, true);
                isMultipleMoving = false;
            }
        }
        else
        {
            if (index == 8)
            {
                //Check Inoput Stuff
            }
            else
            {
                if (currentlySelectedMoleByIndex.Contains(index))
                {
                    currentlySelectedMoleByIndex.Remove(index);
                }
                else
                {
                    currentlySelectedMoleByIndex.Add(index);
                }
                
                if (currentlySelectedMoleByIndex.Count > 1)
                {
                    isSwapping = true;
                    isMultipleMoving = true;
                    int MoleIndex1 = currentlySelectedMoleByIndex[0];
                    int MoleIndex2 = currentlySelectedMoleByIndex[1];
                    Vector3 audioMidPoint = (Buttons[MoleIndex1].transform.position + Buttons[MoleIndex2].transform.position) / 2f;
                    GameObject temp = new GameObject("TempAudioPos");
                    temp.transform.position = audioMidPoint;
                    Audio.PlaySoundAtTransform(AudioOutNames[Rnd.Range(0, 2)], temp.transform);
                    StartCoroutine(SwapMoleAnimation(MoleIndex1, MoleIndex2, temp.transform));
                    GameObject.Destroy(temp, 2f);
                    return;
                }

                StartCoroutine(MoleRaiseHand(MoleList[index], .1f, currentlySelectedMoleByIndex.Contains(index)));
            }
        }
    }

    void MolePopUpHandler(int MoleIndex, bool isGoingUp = false)
    {
        Mole currentMole = MoleList[MoleIndex];
        SetMolePosition(currentMole, isGoingUp);
    }

    void SetMolePosition(Mole mole, bool isUp)
    {
        mole.IsUp = isUp;

        Vector3 target = isUp ? mole.UpPosition : mole.DownPosition;

        if (mole.ActiveMovement != null)
            StopCoroutine(mole.ActiveMovement);

        mole.ActiveMovement = StartCoroutine(MoveMole(mole, target, 0.25f));
    }

    MaterialPair GetRandomMaterialPair()
    {
        int rand1 = Rnd.Range(0, materials.Length);
        int rand2;
        do
        {
            rand2 = Rnd.Range(0, materials.Length);
        } while (rand1 == rand2);
        return new MaterialPair(materials[rand1], materials[rand2]);
    }

    void Solve()
    {
        GetComponent<KMBombModule>().HandlePass();
    }

    void Strike()
    {
        GetComponent<KMBombModule>().HandleStrike();
    }
    /* Delete this if you dont want TP integration
#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} to do something.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
        yield return null;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
    }*/
}
