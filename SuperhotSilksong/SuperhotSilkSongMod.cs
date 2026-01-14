using System;
using BepInEx;
using BepInEx.Configuration;
using InControl;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum TimeControlMode
{
    MovementBased,
    InputBased
}

[BepInPlugin("lagerthon.SuperhotSilkSongMod", "Superhot Silksong Mod", "1.1.1")]
public class SuperhotSilkSongMod : BaseUnityPlugin
{
    private HeroController heroController;

    public static ConfigEntry<KeyCode> toggleKey;
    public static ConfigEntry<TimeControlMode> controlMode;
    public static ConfigEntry<bool> useKeyboard;
    public static ConfigEntry<bool> useController1;
    public static ConfigEntry<bool> useController2;
    public static ConfigEntry<KeyCode> keyLeft;
    public static ConfigEntry<KeyCode> keyRight;
    public static ConfigEntry<KeyCode> keyUp;
    public static ConfigEntry<KeyCode> keyDown;
    public static ConfigEntry<KeyCode> keyJump;
    public static ConfigEntry<KeyCode> keyAttack;
    public static ConfigEntry<KeyCode> keyDash;
    public static ConfigEntry<float> minSpeedMultiplier;
    public static ConfigEntry<float> maxSpeedMultiplier;
    public static ConfigEntry<float> growthFactor;
    public static ConfigEntry<float> decayFactor;
    private bool isGameActive;
    private bool modEnabled = false;
    private bool inputActive;
    private float inputStrength;
    private float joystickIntensity;
    private float inputActiveTime = 0f;
    private float inputInActiveTime = 0f;
    private const int sigmoidResolution = 1000;
    private float[] sigmoidRise;
    private float[] sigmoidFall;
    private int index;
    private float curve;
    private float lastGrowthFactor;
    private float lastDecayFactor;

    private void Awake()
    {
        toggleKey = Config.Bind(
            "General",
            "Enable Mod",
            KeyCode.RightShift,
            "Key used to enable or disable the mod");

        controlMode = Config.Bind(
            "General",
            "Speed Control Mode",
            TimeControlMode.MovementBased,
            "MovementBased = Time scales with Hornet's movement\n" +
            "InputBased = Time scales with keyboard/controller input");

        minSpeedMultiplier = Config.Bind(
            "Speed",
            "Min Speed Multiplier",
            0.2f,
            new ConfigDescription(
                "Minimum game speed when idle",
                new AcceptableValueRange<float>(0.1f, 1f)));

        maxSpeedMultiplier = Config.Bind(
            "Speed",
            "Max Speed Multiplier",
            1.5f,
            new ConfigDescription(
                "Maximum game speed",
                new AcceptableValueRange<float>(1f, 2f)));

        growthFactor = Config.Bind(
            "Tuning",
            "Growth Factor",
            2.5f,
            new ConfigDescription(
                "How quickly time accelerates when movement/input begins",
                new AcceptableValueRange<float>(1f, 5f)));

        decayFactor = Config.Bind(
            "Tuning",
            "Decay Factor",
            2.5f,
            new ConfigDescription(
                "How quickly time decays when idle",
                new AcceptableValueRange<float>(1f, 5f)));

        useKeyboard = Config.Bind(
            "Input",
            "Use Keyboard",
            true,
            "Use keyboard input for InputBased mode");

        keyLeft = Config.Bind(
            "Input.Keyboard",
            "Key 1",
            KeyCode.LeftArrow);

        keyRight = Config.Bind(
            "Input.Keyboard",
            "Key 2",
            KeyCode.RightArrow);

        keyUp = Config.Bind(
            "Input.Keyboard",
            "Key 3",
            KeyCode.UpArrow);

        keyDown = Config.Bind(
            "Input.Keyboard",
            "Key 4",
            KeyCode.DownArrow);

        keyJump = Config.Bind(
            "Input.Keyboard",
            "Key 5",
            KeyCode.Z);

        keyAttack = Config.Bind(
            "Input.Keyboard",
            "Key 6",
            KeyCode.X);

        keyDash = Config.Bind(
            "Input.Keyboard",
            "Key 7",
            KeyCode.C);

        useController1 = Config.Bind(
            "Input",
            "Controller Mode 1",
            false,
            "Time ramps up while stick is held");

        useController2 = Config.Bind(
            "Input",
            "Controller Mode 2 (Analog)",
            false,
            "Time ramps up with stick intensity");

        useKeyboard.SettingChanged += (_, __) =>
        {
            if (useKeyboard.Value)
            {
                useController1.Value = false;
                useController2.Value = false;
            }
        };

        useController1.SettingChanged += (_, __) =>
        {
            if (useController1.Value)
            {
                useKeyboard.Value = false;
                useController2.Value = false;
            }
        };

        useController2.SettingChanged += (_, __) =>
        {
            if (useController2.Value)
            {
                useKeyboard.Value = false;
                useController1.Value = false;
            }
        };

        growthFactor.SettingChanged += (_, __) => SigmoidRise();
        decayFactor.SettingChanged += (_, __) => SigmoidFall();

        SigmoidRise();

        SigmoidFall();

        inputActiveTime = 1f;
        inputInActiveTime = 1f;

        inputActive = false;
        inputStrength = 1f;

        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    private void SigmoidRise()
    {
        if (sigmoidRise == null || sigmoidRise.Length != sigmoidResolution + 1)
            sigmoidRise = new float[sigmoidResolution + 1];

        float mult = 10.0f * growthFactor.Value;

        for (int i = 0; i <= sigmoidResolution; i++)
        {
            float t = i / (float)sigmoidResolution;
            sigmoidRise[i] = 1f / (1f + Mathf.Exp(-(t - 5f / mult) * mult));
        }
    }

    private void SigmoidFall()
    {
        if (sigmoidFall == null || sigmoidFall.Length != sigmoidResolution + 1)
            sigmoidFall = new float[sigmoidResolution + 1];

        float mult = 10.0f * decayFactor.Value;

        for (int i = 0; i <= sigmoidResolution; i++)
        {
            float t = i / (float)sigmoidResolution;
            sigmoidFall[i] = 1f / (1f + Mathf.Exp(-(t - 5f / mult) * mult));
        }
    }

    private void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        heroController = HeroController.instance;
        isGameActive = heroController != null;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey.Value) && heroController != null)
        {
            modEnabled = !modEnabled;

            if (!modEnabled)
            {
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f;
            }
        }

        if (!modEnabled || !isGameActive)
            return;

        if (controlMode.Value != TimeControlMode.InputBased)
            return;

        if (useController1.Value || useController2.Value)
        {
            var device = InputManager.ActiveDevice;
            if (device == null) return;
            Vector2 leftStick = InputManager.ActiveDevice.LeftStick.Value;
            bool buttons = InputManager.ActiveDevice.AnyButton;
            bool leftBumper = InputManager.ActiveDevice.LeftBumper;
            bool rightBumper = InputManager.ActiveDevice.RightBumper;
            bool leftTrigger = InputManager.ActiveDevice.LeftTrigger;
            bool rightTrigger = InputManager.ActiveDevice.RightTrigger;
            joystickIntensity = Mathf.Clamp01(leftStick.magnitude);
            inputActive = (joystickIntensity > 0.05f) ||
                            buttons ||
                            leftBumper ||
                            rightBumper ||
                            leftTrigger ||
                            rightTrigger;
            inputStrength = useController2.Value ? joystickIntensity : 1f;
        }
        else if (useKeyboard.Value)
        {
            inputStrength = 1f;
            inputActive =
                Input.GetKey(keyLeft.Value) ||
                Input.GetKey(keyRight.Value) ||
                Input.GetKey(keyUp.Value) ||
                Input.GetKey(keyDown.Value) ||
                Input.GetKey(keyJump.Value) ||
                Input.GetKey(keyAttack.Value) ||
                Input.GetKey(keyDash.Value);
        }
    }

    private void LateUpdate()
    {
        if (!modEnabled || !isGameActive)
        {
            if (Time.timeScale != 1f)
            {
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f;
            }
            return;
        }

        switch (controlMode.Value)
        {
            case TimeControlMode.MovementBased:
                ApplyVelocityBasedTime();
                break;

            case TimeControlMode.InputBased:
                ApplyInputBasedTime();
                break;
        }
    }
    private void ApplyVelocityBasedTime()
    {
        float velocity = heroController.current_velocity.magnitude;

        float dt = Time.unscaledDeltaTime;

        if (velocity > 0f)
        {
            inputActiveTime += dt;
            inputInActiveTime = 0f;

            if (inputActiveTime > 1f)
                inputActiveTime = 1f;

            float tNormalized = inputActiveTime;
            index = Mathf.RoundToInt(tNormalized * sigmoidResolution);
            curve = sigmoidRise[index];

            Time.timeScale = curve * maxSpeedMultiplier.Value;
        }
        else
        {
            inputInActiveTime += dt;
            inputActiveTime = 0f;

            if (inputInActiveTime > 1f)
                inputInActiveTime = 1f;

            float tNormalized = inputInActiveTime;
            index = Mathf.RoundToInt(tNormalized * sigmoidResolution);
            curve = 1f - sigmoidFall[index];

            Time.timeScale = curve;
        }

        Time.timeScale = Math.Clamp(Time.timeScale, minSpeedMultiplier.Value, maxSpeedMultiplier.Value);
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    private void ApplyInputBasedTime()
    {
        float dt = Time.unscaledDeltaTime;

        if (inputActive)
        {
            inputActiveTime += dt;
            inputInActiveTime = 0f;

            if (inputActiveTime > 1f)
                inputActiveTime = 1f;

            float tNormalized = inputActiveTime;
            index = Mathf.RoundToInt(tNormalized * sigmoidResolution);
            curve = sigmoidRise[index];

            Time.timeScale = curve * inputStrength * maxSpeedMultiplier.Value;
        }
        else
        {
            inputInActiveTime += dt;
            inputActiveTime = 0f;

            if (inputInActiveTime > 1f)
                inputInActiveTime = 1f;

            float tNormalized = inputInActiveTime;
            index = Mathf.RoundToInt(tNormalized * sigmoidResolution);
            curve = 1f - sigmoidFall[index];

            Time.timeScale = curve;
        }

        Time.timeScale = Math.Clamp(Time.timeScale, minSpeedMultiplier.Value, maxSpeedMultiplier.Value);
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
}