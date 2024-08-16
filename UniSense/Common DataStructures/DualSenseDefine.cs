using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// This file contains data structure definitions that are useful when changing the output state of a DualSense controller
/// </summary> 
namespace UniSense
{
    /// <summary>
    /// Encapsulates all output setting that can be changed on a DualSense controller
    /// </summary>
    public struct DualSenseGamepadState
    {
        public Color? LightBarColor;
        public DualSenseMotorSpeed? Motor;
        public DualSenseMicLedState? MicLed;

        public DualSenseTriggerState? RightTrigger;
        public DualSenseTriggerState? LeftTrigger;

        public PlayerLedBrightness? PlayerLedBrightness;
        public PlayerLedState? PlayerLed;
    }

    public struct DualSenseMotorSpeed
    {
        public float LowFrequencyMotorSpeed;
        public float HighFrequenceyMotorSpeed;

        public DualSenseMotorSpeed(float lowFrequencyMotorSpeed, float highFrequenceyMotorSpeed)
        {
            LowFrequencyMotorSpeed = lowFrequencyMotorSpeed;
            HighFrequenceyMotorSpeed = highFrequenceyMotorSpeed;
        }
    }

    public enum DualSenseMicLedState
    {
        Off,
        On,
        Pulsating,
    }

    public enum DualSenseTriggerEffectType : byte
    {
        NoResistance = 0,
        ContinuousResistance,
        SectionResistance,
        EffectEx,
        Calibrate,
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct DualSenseTriggerState
    {
        [FieldOffset(0)] public DualSenseTriggerEffectType EffectType;

        [FieldOffset(1)] public DualSenseContinuousResistanceProperties Continuous;
        [FieldOffset(1)] public DualSenseSectionResistanceProperties Section;
        [FieldOffset(1)] public DualSenseEffectExProperties EffectEx;
    }

    public struct DualSenseContinuousResistanceProperties
    {
        public byte StartPosition;
        public byte Force;
    }

    public struct DualSenseSectionResistanceProperties
    {
        public byte StartPosition;
        public byte EndPosition;
        public byte Force;
    }

    public enum DualSenseTrigger
    {
        Left,
        Right,
    }

    public struct DualSenseEffectExProperties
    {
        public byte StartPosition;
        public bool KeepEffect;
        public byte BeginForce;
        public byte MiddleForce;
        public byte EndForce;
        public byte Frequency;
    }

    public enum PlayerLedBrightness
    {
        High,
        Medium,
        Low,
    }
    public enum PlayerLED : byte
    {
        Player1 = 0x01,
        Player2 = 0x02,
        Player3 = 0x04,
        Player4 = 0x08,
        Player5 = 0x10,
        None = 0x00,
    }

    public struct PlayerLedState
    {
        private const byte LED1 = 0x01;
        private const byte LED5 = 0x10;
        private const byte LED4 = 0x08;
        private const byte LED3 = 0x04;
        private const byte LED2 = 0x02;

        private const byte LED_MASK = LED1 | LED2 | LED3 | LED4 | LED5;

        private const byte FADE = 0x40;

        public byte Value { get; private set; }

        public PlayerLedState(bool led1, bool led2, bool led3, bool led4, bool led5, bool fade = true)
        {
            Value = 0;
            if (led1) Value |= LED1;
            if (led2) Value |= LED2;
            if (led3) Value |= LED3;
            if (led4) Value |= LED4;
            if (led5) Value |= LED5;
            if (fade) Value |= FADE;
        }

        public PlayerLedState(byte led, bool fade = false)
        {
            byte playerLed;
            switch ((PlayerLED)led)
            {
                case PlayerLED.Player5:
                    playerLed = LED_MASK;
                    break;
                case PlayerLED.Player4:
                    playerLed = LED1 | LED2 | LED4 | LED5;
                    break;
                case PlayerLED.Player3:
                    playerLed = LED1 | LED3 | LED5;
                    break;
                case PlayerLED.Player2:
                    playerLed = LED2 | LED4;
                    break;
                case PlayerLED.Player1:
                    playerLed = LED3;
                    break;
                default:
                    playerLed = 0;
                    break;
            }


            Value = (byte)(playerLed & LED_MASK);
            if (fade) Value |= FADE;
        }
    }
}