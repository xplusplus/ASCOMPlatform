﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using ASCOM.Utilities.CS.Interfaces;
using System.Runtime.InteropServices;

namespace ASCOM.Utilities.CS
{
    // Implements the Timer component


    /// <summary>

    /// ''' Provides a repeating timer with associated tick event.

    /// ''' </summary>

    /// ''' <remarks>

    /// ''' <para>The interval resolution is about 20ms.If you need beter than this, you could use the WaitForMilliseconds 

    /// ''' method to create your own solution.</para>

    /// ''' <para>You can create multiple instances of this object. When enabled, the Timer delivers Tick events periodically 

    /// ''' (determined by setting the Interval property).</para>

    /// ''' <para>This component is now considered <b>obsolete</b> for use in .NET clients and drivers. It is reliable under almost 

    /// ''' all circumstances but there are some environments, noteably console and some scripted applications, where it fails to fire.

    /// ''' The Platform 6 component improves performance over the Platform 5 component in this respect and can be further tuned 

    /// ''' for particular applications by placing an entry in the ForceSystemTimer Profile key.</para>

    /// ''' <para>For .NET applications, use of System.Timers.Timer is recommended but atention must be paid to getting threading correct

    /// ''' when using this control. The Windows.Forms.Timer control is not an improvement over the ASCOM timer which is based upon it.</para>

    /// ''' <para>Developers using non .NET languages are advised to use timers provided as part of their development environment, only falling 

    /// ''' back to the ASCOM Timer if no viable alternative can be found.</para>

    /// ''' </remarks>
    [Guid("64FEE414-176D-44d0-99DF-47621D9C377F")]
    [ComVisible(true)]
    [ComSourceInterfaces(typeof(ITimerEvent))]
    [ClassInterface(ClassInterfaceType.None)]
    [Obsolete("Please replace it with Systems.Timers.Timer, which is reliable in all console and non-windowed applications.", false)]
    public class Timer : ITimer, IDisposable
    {
        
        //   =========
        //   TIMER.CLS
        //   =========
        //
        // Implementation of the ASCOM DriverHelper Timer class.
        //
        // Written:  28-Jan-01   Robert B. Denny <rdenny@dc3.com>
        //
        // Edits:
        //
        // When      Who     What
        // --------- ---     --------------------------------------------------
        // 03-Feb-09 pwgs    5.1.0 - refactored for Utilities
        // ---------------------------------------------------------------------

        // Set up a timer to create the tick events. Use a FORMS timer so that it will fire on the current owner//s thread
        // If you use a system timer it will  fire on its own thread and this will be invisble to the application!
        // Private WithEvents m_Timer As System.Windows.Forms.Timer
         
        // NEED TO CONVERT FROM VB
        //     private WithEvents FormTimer As Windows.Forms.Timer
        //     private WithEvents TimersTimer As System.Timers.Timer
        // END NEED TO CONVERT
        private bool IsForm, TraceEnabled;
        private TraceLogger TL;

        /// <summary>
        ///     ''' Timer tick event handler
        ///     ''' </summary>
        ///     ''' <remarks></remarks>
        [ComVisible(false)]
        public delegate void TickEventHandler();

        /// <summary>
        ///     ''' Fired once per Interval when timer is Enabled.
        ///     ''' </summary>
        ///     ''' <remarks>To sink this event in Visual Basic, declare the object variable using the WithEvents keyword.</remarks>
        public event TickEventHandler Tick; // Implements ITimer.Tick ' Declare the tick event
        /// <summary>
        ///     ''' Create a new timer component
        ///     ''' </summary>
        ///     ''' <remarks></remarks>
        public SurroundingClass()
        {
            TL = new TraceLogger("", "Timer");
            TraceEnabled = GetBool(TRACE_TIMER, TRACE_TIMER_DEFAULT);
            TL.Enabled = TraceEnabled;

            TL.LogMessage("New", "Started on thread: " + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString());
            FormTimer = new Windows.Forms.Timer();
            TL.LogMessage("New", "Created FormTimer");
            FormTimer.Enabled = false; // Default settings
            FormTimer.Interval = 1000; // Inital period - 1 second
            TL.LogMessage("New", "Set FormTimer interval");

            TimersTimer = new System.Timers.Timer();
            TL.LogMessage("New", "Created TimersTimer");

            TimersTimer.Enabled = false; // Default settings
            TimersTimer.Interval = 1000; // Inital period - 1 second
            TL.LogMessage("New", "Set TimersTimer interval");

            try
            {
                TL.LogMessage("New", "Process FileName " + "\"" + Process.GetCurrentProcess().MainModule.FileName + "\"");
                PEReader PE = new PEReader(Process.GetCurrentProcess().MainModule.FileName, TL);
                TL.LogMessage("New", "SubSystem " + PE.SubSystem.ToString);
                switch (PE.SubSystem)
                {
                    case object _ when PEReader.SubSystemType.WINDOWS_GUI // Windows GUI app
                   :
                        {
                            IsForm = true;
                            break;
                        }

                    case object _ when PEReader.SubSystemType.WINDOWS_CUI // Windows Console app
             :
                        {
                            IsForm = false;
                            break;
                        }

                    default:
                        {
                            IsForm = false;
                            break;
                        }
                }
                // If Process.GetCurrentProcess.MainModule.FileName.ToUpper.Contains("WSCRIPT.EXE") Then 'WScript is an exception that is marked GUI but behaves like console!
                // TL.LogMessage("New", "WScript.Exe found - Overriding IsForm to: False")
                // IsForm = False
                // End If
                // If Process.GetCurrentProcess.MainModule.FileName.ToUpper.Contains("ASTROART.EXE") Then 'WScript is an exception that is marked GUI but behaves like console!
                // TL.LogMessage("New", "AstroArt.Exe found - Overriding IsForm to: False")
                // IsForm = False
                // End If
                IsForm = !ForceTimer(IsForm); // Override the value of isform if required
                TL.LogMessage("New", "IsForm: " + IsForm);
            }
            catch (Exception ex)
            {
                TL.LogMessageCrLf("New Exception", ex.ToString()); // Log error and record in the event log
                LogEvent("Timer:New", "Exception", EventLogEntryType.Error, EventLogErrors.TimerSetupException, ex.ToString());
            }
        }

        private bool ForceTimer(bool CurrentIsForm)
        {
            RegistryAccess Profile = new RegistryAccess();
            Generic.SortedList<string, string> ForcedSystemTimers;
            string ProcessFileName;
            bool ForceSystemTimer, MatchedName;

            ForceTimer = !CurrentIsForm; // Set up default return value to supplied value. ForceTimer is opposite logic to IsForm, hence use of Not
            TL.LogMessage("ForceTimer", "Current IsForm: " + CurrentIsForm.ToString() + ", this makes the default ForceTimer value: " + ForceTimer);

            ProcessFileName = Process.GetCurrentProcess().MainModule.FileName.ToUpperInvariant(); // Get the current process processname
            TL.LogMessage("ForceTimer", "Main process file name: " + ProcessFileName);

            MatchedName = false;
            ForcedSystemTimers = Profile.EnumProfile(FORCE_SYSTEM_TIMER); // Get the list of applications requiring special timer handling
            foreach (Generic.KeyValuePair<string, string> ForcedFileName in ForcedSystemTimers) // Check each forced file in turn 
            {
                if (ProcessFileName.Contains(Trim(ForcedFileName.Key.ToUpperInvariant)))
                {
                    TL.LogMessage("ForceTimer", "  Found: \"" + ForcedFileName.Key + "\" = \"" + ForcedFileName.Value + "\"");
                    MatchedName = true;
                    if (bool.TryParse(ForcedFileName.Value, out ForceSystemTimer))
                    {
                        ForceTimer = ForceSystemTimer;
                        TL.LogMessage("ForceTimer", "    Parsed OK: " + ForceTimer.ToString() + ", ForceTimer set to: " + ForceTimer);
                    }
                    else
                        TL.LogMessage("ForceTimer", "    ***** Error - Value is not boolean!");
                }
                else
                    TL.LogMessage("ForceTimer", "  Tried: \"" + ForcedFileName.Key + "\" = \"" + ForcedFileName.Value + "\"");
            }
            if (!MatchedName)
                TL.LogMessage("ForceTimer", "  Didn't match any force timer application names");

            TL.LogMessage("ForceTimer", "Returning: " + ForceTimer.ToString());
            return ForceTimer;
        }

        private bool disposedValue = false;        // To detect redundant calls

        // IDisposable
        /// <summary>
        ///     ''' Disposes of resources used by the profile object - called by IDisposable interface
        ///     ''' </summary>
        ///     ''' <param name="disposing"></param>
        ///     ''' <remarks></remarks>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    TL.Enabled = false;
                    TL.Dispose();
                }
                if (!(FormTimer == null))
                {
                    if (!FormTimer == null)
                        FormTimer.Enabled = false;
                    FormTimer.Dispose();
                    FormTimer = null;
                }
            }
            this.disposedValue = true;
        }

        // This code added by Visual Basic to correctly implement the disposable pattern.
        /// <summary>
        ///     ''' Disposes of resources used by the profile object
        ///     ''' </summary>
        ///     ''' <remarks></remarks>
        public void Dispose()
        {
            // Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SurroundingClass()
        {
            // Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
            Dispose(false);
            base.Finalize();
        }


        /// <summary>
        ///     ''' The interval between Tick events when the timer is Enabled in milliseconds, (default = 1000)
        ///     ''' </summary>
        ///     ''' <value>The interval between Tick events when the timer is Enabled (milliseconds, default = 1000)</value>
        ///     ''' <returns>The interval between Tick events when the timer is Enabled in milliseconds</returns>
        ///     ''' <remarks></remarks>
        public int Interval
        {
            // Get and set the timer period
            get
            {
                if (IsForm)
                {
                    TL.LogMessage("Interval FormTimer Get", FormTimer.Interval.ToString + ", Thread: " + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString());
                    return FormTimer.Interval;
                }
                else
                {
                    TL.LogMessage("Interval TimersTimer Get", TimersTimer.Interval.ToString + ", Thread: " + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString());
                    return FormTimer.Interval;
                }
            }
            set
            {
                if (IsForm)
                {
                    TL.LogMessage("Interval FormTimer Set", Value.ToString() + ", Thread: " + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString());
                    if (Value > 0)
                        FormTimer.Interval = Value;
                    else
                        FormTimer.Enabled = false;
                }
                else
                {
                    TL.LogMessage("Interval TimersTimer Set", Value.ToString() + ", Thread: " + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString());
                    if (Value > 0)
                        TimersTimer.Interval = Value;
                    else
                        TimersTimer.Enabled = false;
                }
            }
        }

        /// <summary>
        ///     ''' Enable the timer tick events
        ///     ''' </summary>
        ///     ''' <value>True means the timer is active and will deliver Tick events every Interval milliseconds.</value>
        ///     ''' <returns>Enabled state of timer tick events</returns>
        ///     ''' <remarks></remarks>
        public bool Enabled
        {
            // Enable and disable the timer
            get
            {
                if (IsForm)
                {
                    TL.LogMessage("Enabled FormTimer Get", FormTimer.Enabled.ToString + ", Thread: " + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString());
                    return FormTimer.Enabled;
                }
                else
                {
                    TL.LogMessage("Enabled TimersTimer Get", TimersTimer.Enabled.ToString + ", Thread: " + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString());
                    return TimersTimer.Enabled;
                }
            }
            set
            {
                if (IsForm)
                {
                    TL.LogMessage("Enabled FormTimer Set", Value.ToString() + ", Thread: " + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString());
                    FormTimer.Enabled = Value;
                }
                else
                {
                    TL.LogMessage("Enabled TimersTimer Set", Value.ToString() + ", Thread: " + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString());
                    TimersTimer.Enabled = Value;
                }
            }
        }

        // Raise the external event whenever a timer tick event occurs
        /// <summary>
        ///     ''' Timer event handler
        ///     ''' </summary>
        ///     ''' <remarks>Raises the Tick event</remarks>
        private void OnTimedEvent(object sender, object e)
        {
            if (IsForm)
            {
                System.EventArgs ec;
                ec = (System.EventArgs)e;
            }
            else
            {
                System.Timers.ElapsedEventArgs ec;
                ec = (System.Timers.ElapsedEventArgs)e;
                if (TraceEnabled)
                    TL.LogMessage("OnTimedEvent", "SignalTime: " + ec.SignalTime.ToString());
            }
            if (TraceEnabled)
                TL.LogMessage("OnTimedEvent", "Raising Tick" + ", Thread: " + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString()); // Ensure minimum hit on timing under normal, non-trace conditions
            Tick?.Invoke();
            if (TraceEnabled)
                TL.LogMessage("OnTimedEvent", "Raised Tick" + ", Thread: " + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString()); // Ensure minimum hit on timing under normal, non-trace, conditions
        }

        private void Timer_Tick()
        {
        }
    }

}
