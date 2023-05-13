#region Assembly Unity.InputSystem, Version=1.4.4.0, Culture=neutral, PublicKeyToken=null
 //E:\VideoGameProject\UniSense\Library\ScriptAssemblies\Unity.InputSystem.dll
#endregion
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.DualShock.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem;
using UnityEngine.Scripting;
using UniSense.LowLevel;
namespace UnityEngine.InputSystem.DualSense
{



    /// <summary>
    /// A Sony DualShock/DualSense controller.
    /// </summary>
    [InputControlLayout(displayName = "DualSense (UniSense)")]
    public class UnisenseDualSenseGamepad : DualShockGamepad
    {
        /// <summary>
        /// Button that is triggered when the left trigger is pressed
        /// </summary>
        /// <value>Control representing the left trigger </value>
        [InputControl(name = "leftTriggerButton", displayName = "Left Trigger Button", shortDisplayName = "L2 Button")]
        public ButtonControl leftTriggerButton { get; private set; }
        /// <summary>
        /// Button that is triggered when the right trigger is pressed
        /// </summary>
        /// <value>Control representing the right trigger </value>
        [InputControl(name = "rightTriggerButton", displayName = "Right Trigger Button", shortDisplayName = "R2 Button")]
        public ButtonControl rightTriggerButton { get; private set; }
        
        /// <summary>
        /// The Playstation logo Button
        /// </summary>
        [InputControl(name = "systemButton", displayName = "PS Button", shortDisplayName = "PS")]
        public ButtonControl systemButton { get; private set; }

        /// <summary>
        /// The small button below the Playstation logo button
        /// </summary>
        [InputControl(name = "micMuteButton", displayName = "Mic Mute Button", shortDisplayName = "Mic Mute")]
        public ButtonControl micMuteButton { get; private set; }
       
        /// <summary>
        /// Control representing the gyroscope present in the DualSense controller
        /// </summary>
        /// <value>Reports the angular velocity of the controller</value>
        [InputControl(name = "gyro", displayName = "Angular Velocity (gyroscope)", shortDisplayName = "Gyro")]
        public Vector3Control angularVelocity { get; private set; }

        /// <summary>
        /// Control representing the accelerometer present in the DualSense controller
        /// </summary>
        /// <value>Reports the linear acceleration of the DualSense controller</value>
        [InputControl(name = "accel", displayName = "Acceleration (accelerometer)", shortDisplayName = "Accel")]
        public Vector3Control acceleration { get; private set; }
       
        /// <summary>
        /// Control representing the charging status of the DualSense controller
        /// </summary>
        /// <value>Is pressed when controller is plugged in</value>
        [InputControl(name = "batteryCharging", displayName = "Battery Charging", shortDisplayName = "Charging")]
        public ButtonControl batteryCharging { get; private set; }
       
        //TODO:See if I can fix the battery status
        /// <summary>
        /// Control representing if the controller is fully charged
        /// </summary>
        /// <value>Is broken will always not be pressed</value>
        [InputControl(name = "batteryFullyCharged", displayName = "Battery Fully Charged", shortDisplayName = "Fully Charged")]
        public ButtonControl batteryFullyCharged { get; private set; }

        /// <summary>
        /// Control representing the charge status of the controller
        /// </summary>
        /// <value>Is broken will always not be pressed</value>
        [InputControl(name = "batteryLevel", displayName = "Battery Level", shortDisplayName = "Battery Level")]
        public AxisControl batteryLevl { get; private set; }
       
        public new static UnisenseDualSenseGamepad current { get; private set; }

            /// <inheritdoc />
            public override void MakeCurrent()
            {
                base.MakeCurrent();
                current = this;
            }

            /// <inheritdoc />
            protected override void OnRemoved()
            {
                base.OnRemoved();
                if (current == this)
                    current = null;
            }

            /// <inheritdoc />
            protected override void FinishSetup()
            {
                base.FinishSetup();
            }

            /// <inheritdoc />
            public virtual void SetLightBarColor(Color color)
            {
            }
    }
    


}

