﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

using Majorsoft.Blazor.Components.Timer;

namespace Majorsoft.Blazor.Components.Debounce
{
	/// <summary>
	/// Base class for Debounce components.
	/// </summary>
	public abstract class DebounceTimerBase : ComponentBase
	{
		private bool _notifiedLastChange = false;
		private bool _debounceEnabled = true;

		protected AdvancedTimer _timer;
		protected ElementReference _inputRef;
		/// <summary>
		/// Exposes a Blazor ElementReference of the wrapped around HTML element. It can be used e.g. for JS interop, etc.
		/// </summary>
		public ElementReference InnerElementReference => _inputRef;

		private double _intervalInMilisec = 200;
		/// <summary>
		/// Notification debounce timeout in ms. If set to 0 notifications happens immediately. -1 disables automatic notification completely. 
		/// Notification will only happen by pressing Enter key or onblur, if set.
		/// </summary>
		[Parameter] public double DebounceTime
		{
			get => _intervalInMilisec;
			set
			{
				if (value == _intervalInMilisec)
				{
					return;
				}

				SetTimer(value);
			}
		}

		/// <summary>
		/// Value of the rendered HTML element. Initial field value can be set to given string or omitted (leave empty). 
		/// Also control actual value can be read out (useful when MinLenght not reached).
		/// </summary>
		[Parameter] public string? Value { get; set; }
		/// <summary>
		/// Minimal length of text to start notify, if value is shorter than MinLength, there will be notifications with empty value "".
		/// </summary>
		[Parameter] public int MinLength { get; set; } = 0;
		/// <summary>
		/// Notification of current value will be sent immediately by hitting Enter key. Enabled by-default. 
		/// Notification will obey MinLength rule, if length is less, then empty value "" will be sent back.
		/// </summary>
		[Parameter] public bool ForceNotifyByEnter { get; set; } = true;
		/// <summary>
		/// Same as ForceNotifyByEnter but notification triggered onblur event, when focus leaves the input field.
		/// </summary>
		[Parameter] public bool ForceNotifyOnBlur { get; set; } = true;

		//Events
		/// <summary>
		/// Callback function called when HTML control received keyboard inputs.
		/// </summary>
		[Parameter] public EventCallback<string> OnInput { get; set; }
		/// <summary>
		/// Callback function called when value was changed (debounced) with field value passed into.
		/// </summary>
		[Parameter] public EventCallback<string> OnValueChanged { get; set; }

		[Parameter(CaptureUnmatchedValues = true)]
		public Dictionary<string, object> AdditionalAttributes { get; set; }

		protected abstract ILogger BaseLogger { get; }

		protected override void OnInitialized()
		{
			SetTimer(DebounceTime);

			WriteDiag($"Initialized with Value: '{Value}', Timer interval: '{DebounceTime}' ms, MinLength: '{MinLength}', DebounceEnabled: '{_debounceEnabled}'.");
			base.OnInitialized();
		}

		protected async Task OnTextChange(ChangeEventArgs e)
		{
			WriteDiag($"OnTextChange event: '{e.Value}', DebounceEnabled: '{_debounceEnabled}', timer interval: '{_intervalInMilisec}'.");
			_timer.Stop(); //Stop previous timer

			if(OnInput.HasDelegate) //Immediately notify listeners of text change e.g. @bind
			{
				await OnInput.InvokeAsync(e.Value?.ToString());
			}

			Value = e.Value?.ToString();
			_notifiedLastChange = false;

			if(_intervalInMilisec == 0) //Notify immediately
			{
				Notify();
			}

			if (!_debounceEnabled) //Do not notify
			{
				return;
			}

			_timer.Reset(); //Re-start timer
		}
		protected void OnBlur(FocusEventArgs e)
		{
			WriteDiag($"OnBlur event: '{e.Type}', ForceNotifyOnBlur: '{ForceNotifyOnBlur}', DebounceEnabled: '{_debounceEnabled}'.");

			if (ForceNotifyOnBlur)
			{
				_timer.Stop(); //Stop timer
				Notify();
			}
		}
		protected void OnKeyPress(KeyboardEventArgs e)
		{
			WriteDiag($"OnKeyPress event: '{e.Key}', ForceNotifyByEnter: '{ForceNotifyByEnter}', DebounceEnabled: '{_debounceEnabled}'.");

			if (ForceNotifyByEnter && (e.Key?.Equals("Enter", StringComparison.OrdinalIgnoreCase) ?? false))
			{
				_timer.Stop(); //Stop timer
				Notify();
			}
		}

		protected void OnElapsed(ulong count)
		{
			WriteDiag($"Timer triggered after: '{DebounceTime}' ms delay, DebounceEnabled: '{_debounceEnabled}', Value: '{Value}'.");
			if (_debounceEnabled)
			{
				Notify();
			}
		}

		private void Notify()
		{
			if(_notifiedLastChange)
			{
				WriteDiag($"Notify event was already sent NotifiedLastChange: '{_notifiedLastChange}'.");
				return;
			}

			WriteDiag($"Start ValueChanged notification with length check. Value: '{Value}' length is {Value?.Length}, required MinLength: '{MinLength}'.");
			var invokeValue = Value?.Length >= MinLength
				? Value
				: string.Empty;

			InvokeAsync(async () =>
			{
				WriteDiag($"Invoke ValueChanged event with: '{invokeValue}'.");

				_notifiedLastChange = true;
				await OnValueChanged.InvokeAsync(invokeValue);
			});
		}

		private void SetTimer(double value)
		{
			if (_timer is not null)
			{
				_timer.Stop();
			}

			_intervalInMilisec = value;
			if (value <= -1)
			{
				_timer?.Stop();
				_debounceEnabled = false;
				return;
			}

			_debounceEnabled = true;
		}

		private void WriteDiag(string message)
		{
			BaseLogger.LogDebug($"Component {this.GetType()}: {message}");
		}
	}
}