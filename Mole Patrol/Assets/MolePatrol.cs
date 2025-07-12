using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using ZT = ZToolsKtane;
using Rnd = UnityEngine.Random;
using Math = ExMath;
using UnityEditor.UI;

public class MolePatrol : MonoBehaviour
{

    public KMBombInfo Bomb;
    public KMAudio Audio;

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    public KMSelectable[] Buttons;

    Dictionary<Color, Color> MoleColors;
    List<Mole> MoleList = new List<Mole>();
    public List<GameObject> MoleGObjects;


    public enum Positions { TL, T, TR, R, DR, D, DL, L}

    public class Mole
    {
        public Positions CurrentPosition;
        public bool IsUp;
        public bool IsLookUp;
        public bool IsHandUp;
        public Color MainColor;
        public Color SecColor;
        public Material MainMat;
        public Material SecMat;
        public Transform Transfrom;
        public Vector3 DownPosition;
        public Vector3 UpPosition;
        public Coroutine ActiveMovement;
        public SkinnedMeshRenderer SMRLookUp;
        public SkinnedMeshRenderer SMRHandUp;

        public Mole(GameObject gameObject)
        {
            CurrentPosition = new Positions();
            IsUp = false;
            IsLookUp = false;
            IsHandUp = false;
            MainColor = new Color();
            SecColor = new Color();
            SMRLookUp = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>()[1];
            SMRHandUp = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>()[0];
            Transfrom = gameObject.transform;
            DownPosition = gameObject.transform.localPosition;
            UpPosition = gameObject.transform.localPosition + new Vector3(0, 0, 0.0015f);
        }
    }

    IEnumerator MoveMole(Mole mole, Vector3 target, float duration)
    {
        Vector3 start = mole.Transfrom.localPosition;
        float elapsed = 0f;

        Vector3 direction = target - start;
        bool goingUp = direction.z > 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            mole.Transfrom.localPosition = Vector3.Lerp(start, target, t);

            float weight = goingUp ? Mathf.Lerp(0f, 100f, t) : Mathf.Lerp(100f, 0f, t);
            mole.SMRLookUp.SetBlendShapeWeight(0, weight);

            yield return null;
        }

        mole.Transfrom.localPosition = target;
        mole.SMRLookUp.SetBlendShapeWeight(0, goingUp ? 100f : 0f);
    }

    void Awake()
    { //Avoid doing calculations in here regarding edgework. Just use this for setting up buttons for simplicity.
        ModuleId = ModuleIdCounter++;
        GetComponent<KMBombModule>().OnActivate += Activate;
        foreach (KMSelectable button in Buttons)
        {
            button.OnInteract += delegate () { InputHandler(button); return false; };
            button.OnHighlight += delegate () { OnHighlight(button);};
            button.OnHighlightEnded += delegate () { OnDeHighlight(button); };
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

        //Setup Colors
        MoleColors = new Dictionary<Color, Color>
        {
            { new Color(1f, 0f, 0f),     new Color(1f, 0.3f, 0.3f) },
            { new Color(0f, 1f, 0f),     new Color(0.3f, 1f, 0.3f) },
            { new Color(0f, 0f, 1f),     new Color(0.3f, 0.3f, 1f) },
            { new Color(1f, 0.5f, 0f),   new Color(1f, 0.7f, 0.3f) },
            { new Color(0.5f, 0.5f, 0.5f), new Color(0.7f, 0.7f, 0.7f) },
            { new Color(1f, 0f, 1f),     new Color(1f, 0.3f, 1f) },
            { new Color(0f, 1f, 1f),     new Color(0.3f, 1f, 1f) }
        };

        //Setup Moles
        for (int i = 0; i < MoleGObjects.Count; i++)
            MoleList.Add(new Mole(MoleGObjects[i]));
    }

    void Update()
    { //Shit that happens at any point after initialization
        if (ModuleSolved) return;
        
    }

    void InputHandler(KMSelectable button)
    {
        int index = Array.IndexOf(Buttons, button);
    }

    void OnHighlight(KMSelectable button)
    {
        int index = Array.IndexOf(Buttons, button);
        if (index == 8) return;
        Mole currentMole = MoleList[index];
        SetMoleUp(currentMole, true);
    }

    void OnDeHighlight(KMSelectable button)
    {
        int index = Array.IndexOf(Buttons, button);
        if (index == 8) return;
        Mole currentMole = MoleList[index];
        SetMoleUp(currentMole, false);
    }

    void SetMoleUp(Mole mole, bool up)
    {
        mole.IsUp = up;

        Vector3 target = up ? mole.UpPosition : mole.DownPosition;

        if (mole.ActiveMovement != null)
            StopCoroutine(mole.ActiveMovement);

        mole.ActiveMovement = StartCoroutine(MoveMole(mole, target, 0.25f));
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
