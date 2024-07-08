using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace ag.WPF.NumericBox
{
    /// <summary>
    /// Represents custom control for input and formatted output of decimal values
    /// </summary>
    /// 
    #region Named parts
    [TemplatePart(Name = _elementText, Type = typeof(TextBox))]
    #endregion

    public class NumericBox : Control
#nullable disable
    {
        private enum CurrentKey
        {
            None,
            Number,
            Delete,
            Back,
            Decimal
        }

        private struct CurrentPosition
        {
            public CurrentKey Key;
            public int Offset;
            public bool Exclude;
        }

        #region Constants
        private const string _elementText = "PART_Text";
        #endregion

        #region Elements
        private TextBox _textBox;
        #endregion

        private CurrentPosition _position;
        private bool _gotFocus;

        #region Dependency properties
        /// <summary>
        /// The identifier of the <see cref="Value"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(decimal?), typeof(NumericBox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));
        /// <summary>
        /// The identifier of the <see cref="DecimalPlaces"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DecimalPlacesProperty = DependencyProperty.Register(nameof(DecimalPlaces), typeof(uint), typeof(NumericBox),
                new FrameworkPropertyMetadata((uint)0, OnDecimalPlacesChanged));
        /// <summary>
        /// The identifier of the <see cref="UseGroupSeparator"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty UseGroupSeparatorProperty = DependencyProperty.Register(nameof(UseGroupSeparator), typeof(bool), typeof(NumericBox),
                new FrameworkPropertyMetadata(true, OnUseGroupSeparatorChanged));
        /// <summary>
        /// The identifier of the <see cref="NegativeForeground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty NegativeForegroundProperty = DependencyProperty.Register(nameof(NegativeForeground), typeof(SolidColorBrush), typeof(NumericBox),
                new FrameworkPropertyMetadata(Brushes.Red, OnNegativeForegroundChanged));
        /// <summary>
        /// The identifier of the <see cref="TextAlignment"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextAlignmentProperty = DependencyProperty.Register(nameof(TextAlignment), typeof(TextAlignment), typeof(NumericBox),
                new FrameworkPropertyMetadata(TextAlignment.Left, OnTextAlignmentChanged));
        /// <summary>
        /// The identifier of the <see cref="IsReadOnly"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(NumericBox),
                new FrameworkPropertyMetadata(false, OnIsReadOnlyChanged));
        /// <summary>
        /// The identifier of the <see cref="ShowTrailingZeros"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowTrailingZerosProperty = DependencyProperty.Register(nameof(ShowTrailingZeros), typeof(bool), typeof(NumericBox),
                new FrameworkPropertyMetadata(true, OnShowTrailingZerosChanged));
        /// <summary>
        /// The identifier of the <see cref="TruncateFractionalPart"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TruncateFractionalPartProperty = DependencyProperty.Register(nameof(TruncateFractionalPart), typeof(bool), typeof(NumericBox),
                new FrameworkPropertyMetadata(true, OnTruncateFractionalPartChanged));
        /// <summary>
        /// The identifier of the <see cref="Text"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(NumericBox),
                new FrameworkPropertyMetadata("", OnTextChanged));

        #endregion

        #region Public properties
        /// <summary>
        /// Gets or sets the string representation of <see cref="Value"/> property.
        /// </summary>
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set
            {
                if (!string.IsNullOrEmpty(value) && !decimal.TryParse(value, out _) && !value.In(CultureInfo.CurrentCulture.NumberFormat.NegativeSign, CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator,$"{CultureInfo.CurrentCulture.NumberFormat.NegativeSign}{CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator}"))
                {
                    throw new FormatException("Input string was not in a correct format.");
                }
                SetValue(TextProperty, !string.IsNullOrEmpty(value) ? value : null);
            }
        }
        /// <summary>
        /// Gets or sets the property specified whether fractional part of decimal value will be truncated (True) accordingly to <see cref="DecimalPlaces"/> or rounded (False).
        /// </summary>
        public bool TruncateFractionalPart
        {
            get => (bool)GetValue(TruncateFractionalPartProperty);
            set => SetValue(TruncateFractionalPartProperty, value);
        }
        /// <summary>
        /// Gets or sets the text alignment of NumericBox.
        /// </summary>
        public TextAlignment TextAlignment
        {
            get => (TextAlignment)GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets the value of NumericBox.
        /// </summary>
        public decimal? Value
        {
            get => (decimal?)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        /// <summary>
        /// Gets or sets the value that indicates whether group separator is used for number formatting.
        /// </summary>
        public bool UseGroupSeparator
        {
            get => (bool)GetValue(UseGroupSeparatorProperty);
            set => SetValue(UseGroupSeparatorProperty, value);
        }

        /// <summary>
        /// Gets or sets the value that indicates the count of decimal digits shown at NumericBox.
        /// </summary>
        public uint DecimalPlaces
        {
            get => (uint)GetValue(DecimalPlacesProperty);
            set => SetValue(DecimalPlacesProperty, value);
        }

        /// <summary>
        /// Gets or sets the Brush to apply to the text contents of NumericBox when control's value is negative.
        /// </summary>
        public SolidColorBrush NegativeForeground
        {
            get => (SolidColorBrush)GetValue(NegativeForegroundProperty);
            set => SetValue(NegativeForegroundProperty, value);
        }
        /// <summary>
        /// Gets or sets the value that indicates whether NumericBox is in read-only state.
        /// </summary>
        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }
        /// <summary>
        /// Gets or sets the value that indicates whether trailing zeroes in decimal part of NumericBox should be shown.
        /// </summary>
        public bool ShowTrailingZeros
        {
            get => (bool)GetValue(ShowTrailingZerosProperty);
            set => SetValue(ShowTrailingZerosProperty, value);
        }
        #endregion

        #region Callbacks
        /// <summary>
        /// Invoked just before the <see cref="TextAlignmentChanged"/> event is raised on NumericBox
        /// </summary>
        /// <param name="oldValue">Old value</param>
        /// <param name="newValue">New value</param>
        private void OnTextAlignmentChanged(TextAlignment oldValue, TextAlignment newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<TextAlignment>(oldValue, newValue)
            {
                RoutedEvent = TextAlignmentChangedEvent
            };
            RaiseEvent(e);
        }

        private static void OnTextAlignmentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not NumericBox box) return;
            box.OnTextAlignmentChanged((TextAlignment)(e.OldValue), (TextAlignment)(e.NewValue));
        }

        /// <summary>
        /// Invoked just before the <see cref="ValueChanged"/> event is raised on NumericBox
        /// </summary>
        /// <param name="oldValue">Old value</param>
        /// <param name="newValue">New value</param>
        private void OnValueChanged(decimal oldValue, decimal newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<decimal>(oldValue, newValue)
            {
                RoutedEvent = ValueChangedEvent
            };
            RaiseEvent(e);
        }

        private static void OnValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not NumericBox box) return;
            box.OnValueChanged(Convert.ToDecimal(e.OldValue), Convert.ToDecimal(e.NewValue));
        }

        /// <summary>
        /// Invoked just before the <see cref="DecimalPlacesChanged"/> event is raised on NumericBox
        /// </summary>
        /// <param name="oldValue">Old decimal digits count</param>
        /// <param name="newValue">New decimal digits count</param>
        private void OnDecimalPlacesChanged(uint oldValue, uint newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<uint>(oldValue, newValue)
            {
                RoutedEvent = DecimalPlacesChangedEvent
            };
            RaiseEvent(e);
        }

        private static void OnDecimalPlacesChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not NumericBox box) return;
            box.OnDecimalPlacesChanged((uint)e.OldValue, (uint)e.NewValue);
        }

        /// <summary>
        /// Invoked just before the <see cref="UseGroupSeparatorChanged"/> event is raised on NumericBox
        /// </summary>
        /// <param name="oldValue">Old value</param>
        /// <param name="newValue">New value</param>
        private void OnUseGroupSeparatorChanged(bool oldValue, bool newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<bool>(oldValue, newValue)
            {
                RoutedEvent = UseGroupSeparatorChangedEvent
            };
            RaiseEvent(e);
        }

        private static void OnUseGroupSeparatorChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not NumericBox box) return;
            box.OnUseGroupSeparatorChanged((bool)e.OldValue, (bool)e.NewValue);
        }

        /// <summary>
        /// Invoked just before the <see cref="NegativeForegroundChanged"/> event is raised on NumericBox
        /// </summary>
        /// <param name="oldValue">Old foreground</param>
        /// <param name="newValue">New foreground</param>
        private void OnNegativeForegroundChanged(SolidColorBrush oldValue, SolidColorBrush newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<SolidColorBrush>(oldValue, newValue)
            {
                RoutedEvent = NegativeForegroundChangedEvent
            };
            RaiseEvent(e);
        }

        private static void OnNegativeForegroundChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not NumericBox box) return;
            box.OnNegativeForegroundChanged((SolidColorBrush)e.OldValue, (SolidColorBrush)e.NewValue);
        }

        /// <summary>
        /// Invoked just before the <see cref="IsReadOnlyChanged"/> event is raised on NumericBox
        /// </summary>
        /// <param name="oldValue">Old value</param>
        /// <param name="newValue">New value</param>
        private void OnIsReadOnlyChanged(bool oldValue, bool newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<bool>(oldValue, newValue)
            {
                RoutedEvent = IsReadOnlyChangedEvent
            };
            RaiseEvent(e);
        }
        private static void OnIsReadOnlyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not NumericBox box) return;
            box.OnIsReadOnlyChanged((bool)e.OldValue, (bool)e.NewValue);
        }

        /// <summary>
        /// Invoked just before the <see cref="ShowTrailingZerosChanged"/> event is raised on NumericBox
        /// </summary>
        /// <param name="oldValue">Old value</param>
        /// <param name="newValue">New value</param>
        private void OnShowTrailingZerosChanged(bool oldValue, bool newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<bool>(oldValue, newValue)
            {
                RoutedEvent = ShowTrailingZerosChangedEvent
            };
            RaiseEvent(e);
        }
        private static void OnShowTrailingZerosChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not NumericBox box) return;
            box.OnShowTrailingZerosChanged((bool)e.OldValue, (bool)e.NewValue);
        }

        /// <summary>
        /// Invoked just before the <see cref="TextChanged"/> event is raised on NumericBox
        /// </summary>
        /// <param name="oldValue">Old value</param>
        /// <param name="newValue">New value</param>
        private void OnTextChanged(string oldValue, string newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<string>(oldValue, newValue)
            {
                RoutedEvent = TextChangedEvent
            };
            RaiseEvent(e);
        }
        private static void OnTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not NumericBox box) return;
            if (box._textBox != null)
                box._textBox.Text = (string)e.NewValue;
            box.OnTextChanged((string)e.OldValue, (string)e.NewValue);
        }

        /// <summary>
        /// Invoked just before the <see cref="TruncateFractionalPartChanged"/> event is raised on NumericBox
        /// </summary>
        /// <param name="oldValue">Old value</param>
        /// <param name="newValue">New value</param>
        private void OnTruncateFractionalPartChanged(bool oldValue, bool newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<bool>(oldValue, newValue)
            {
                RoutedEvent = TruncateFractionalPartChangedEvent
            };
            RaiseEvent(e);
        }
        private static void OnTruncateFractionalPartChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not NumericBox box) return;
            box.OnTruncateFractionalPartChanged((bool)e.OldValue, (bool)e.NewValue);
        }
        #endregion

        #region Routed events
        /// <summary>
        /// Occurs when the <see cref="TextAlignment"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<TextAlignment> TextAlignmentChanged
        {
            add => AddHandler(TextAlignmentChangedEvent, value);
            remove => RemoveHandler(TextAlignmentChangedEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="TextAlignmentChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent TextAlignmentChangedEvent = EventManager.RegisterRoutedEvent("TextAlignmentChanged",
            RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<TextAlignment>), typeof(NumericBox));

        /// <summary>
        /// Occurs when the <see cref="Value"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<decimal> ValueChanged
        {
            add => AddHandler(ValueChangedEvent, value);
            remove => RemoveHandler(ValueChangedEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="ValueChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent("ValueChanged",
            RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<decimal>), typeof(NumericBox));

        /// <summary>
        /// Occurs when the <see cref="UseGroupSeparator"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<bool> UseGroupSeparatorChanged
        {
            add => AddHandler(UseGroupSeparatorChangedEvent, value);
            remove => RemoveHandler(UseGroupSeparatorChangedEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="UseGroupSeparatorChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent UseGroupSeparatorChangedEvent = EventManager.RegisterRoutedEvent("UseGroupSeparatorChanged",
            RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<bool>), typeof(NumericBox));

        /// <summary>
        /// Occurs when the <see cref="DecimalPlaces"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<uint> DecimalPlacesChanged
        {
            add => AddHandler(DecimalPlacesChangedEvent, value);
            remove => RemoveHandler(DecimalPlacesChangedEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="DecimalPlacesChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent DecimalPlacesChangedEvent = EventManager.RegisterRoutedEvent("DecimalPlacesChanged",
            RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<uint>), typeof(NumericBox));

        /// <summary>
        /// Occurs when the <see cref="NegativeForeground"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<SolidColorBrush> NegativeForegroundChanged
        {
            add => AddHandler(NegativeForegroundChangedEvent, value);
            remove => RemoveHandler(NegativeForegroundChangedEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="NegativeForegroundChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent NegativeForegroundChangedEvent = EventManager.RegisterRoutedEvent("NegativeForegroundChanged",
            RoutingStrategy.Direct, typeof(RoutedPropertyChangedEventHandler<SolidColorBrush>), typeof(NumericBox));

        /// <summary>
        /// Occurs when the <see cref="IsReadOnly"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<bool> IsReadOnlyChanged
        {
            add => AddHandler(IsReadOnlyChangedEvent, value);
            remove => RemoveHandler(IsReadOnlyChangedEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="IsReadOnlyChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent IsReadOnlyChangedEvent = EventManager.RegisterRoutedEvent("IsReadOnlyChanged",
            RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<bool>), typeof(NumericBox));

        /// <summary>
        /// Occurs when the <see cref="ShowTrailingZeros"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<bool> ShowTrailingZerosChanged
        {
            add => AddHandler(ShowTrailingZerosChangedEvent, value);
            remove => RemoveHandler(ShowTrailingZerosChangedEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="ShowTrailingZerosChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent ShowTrailingZerosChangedEvent = EventManager.RegisterRoutedEvent("ShowTrailingZerosChanged",
            RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<bool>), typeof(NumericBox));

        /// <summary>
        /// Occurs when the <see cref="TruncateFractionalPart"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<bool> TruncateFractionalPartChanged
        {
            add => AddHandler(TruncateFractionalPartChangedEvent, value);
            remove => RemoveHandler(TruncateFractionalPartChangedEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="TruncateFractionalPartChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent TruncateFractionalPartChangedEvent = EventManager.RegisterRoutedEvent("TruncateFractionalPartChanged",
            RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<bool>), typeof(NumericBox));

        /// <summary>
        /// Occurs when the <see cref="Text"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<string> TextChanged
        {
            add => AddHandler(TextChangedEvent, value);
            remove => RemoveHandler(TextChangedEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="TextChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent TextChangedEvent = EventManager.RegisterRoutedEvent("TextChanged",
            RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<string>), typeof(NumericBox));

        #endregion

        #region ctor
        static NumericBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NumericBox), new FrameworkPropertyMetadata(typeof(NumericBox)));
        }
        #endregion

        #region Overrides
        /// <summary>
        /// Is invoked whenever application code or internal processes call ApplyTemplate.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (_textBox != null)
            {
                _textBox.LostFocus -= textBox_LostFocus;
                _textBox.GotFocus -= textBox_GotFocus;
                _textBox.PreviewKeyDown -= textBox_PreviewKeyDown;
                _textBox.PreviewTextInput -= textBox_PreviewTextInput;
                _textBox.TextChanged -= textBox_TextChanged;
                _textBox.PreviewMouseLeftButtonDown -= textBox_PreviewMouseLeftButtonDown;
                _textBox.CommandBindings.Clear();
                BindingOperations.ClearBinding(this, TextProperty);
            }
            _textBox = GetTemplateChild(_elementText) as TextBox;
            if (_textBox != null)
            {
                _textBox.LostFocus += textBox_LostFocus;
                _textBox.GotFocus += textBox_GotFocus;
                _textBox.PreviewKeyDown += textBox_PreviewKeyDown;
                _textBox.PreviewTextInput += textBox_PreviewTextInput;
                _textBox.TextChanged += textBox_TextChanged;
                _textBox.PreviewMouseLeftButtonDown += textBox_PreviewMouseLeftButtonDown;
                _textBox.CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, pasteCommandBinding));
                _textBox.CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, cutCommandBinding));
                BindingOperations.SetBinding(this, TextProperty, new Binding("Text") { Source = _textBox, Mode = BindingMode.OneWay });
            }
        }
        #endregion

        #region Event handlers
        private void textBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                _textBox.SelectAll();
        }

        private void textBox_LostFocus(object sender, RoutedEventArgs e)
        {
            _gotFocus = false;
            if (_textBox.Text == CultureInfo.CurrentCulture.NumberFormat.NegativeSign)
            {
                Value = null;
            }
            else if (_textBox.Text.EndsWith(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator) && !ShowTrailingZeros)
            {
                _textBox.Text = _textBox.Text.Substring(0, _textBox.Text.Length - 1);
            }
        }

        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Text = _textBox.Text;
            if (!ShowTrailingZeros)
            {
                if (_gotFocus)
                {
                    _textBox.SelectAll();
                    _gotFocus = false;
                }
                return;
            }

            if (_position.Exclude)
                return;
            if (_position.Key.In(CurrentKey.Number, CurrentKey.Back, CurrentKey.Decimal))
            {
                if (_textBox.Text.Length >= _position.Offset)
                {
                    _textBox.CaretIndex = _textBox.Text.Length - _position.Offset;
                }
            }
        }

        private void textBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                _gotFocus = false;
                if (e.Text == CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator)
                {
                    e.Handled = true;
                    return;
                }
                else if (e.Text == CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                {
                    if (DecimalPlaces == 0)
                    {
                        e.Handled = true;
                        return;
                    }

                    if (ShowTrailingZeros)
                    {
                        if (_textBox.Text != CultureInfo.CurrentCulture.NumberFormat.NegativeSign)
                        {
                            _textBox.CaretIndex = _textBox.Text.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator) + 1;
                            e.Handled = true;
                        }
                        else
                        {
                            _position.Key = CurrentKey.Decimal;
                        }
                    }
                    else
                    {
                        if (_textBox.Text.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, StringComparison.Ordinal) == -1)
                        {
                            _position.Key = CurrentKey.Decimal;
                        }
                        else
                        {
                            _textBox.CaretIndex = _textBox.Text.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator) + 1;
                            e.Handled = true;
                        }
                    }
                    return;
                }
                else if (e.Text == CultureInfo.CurrentCulture.NumberFormat.NegativeSign)
                {
                    if (_textBox.SelectionLength == _textBox.Text.Length)
                    {
                        return;
                    }
                    if (_textBox.CaretIndex > 0)
                    {
                        e.Handled = true;
                        return;
                    }
                }
                else if (!e.Text.In("0", "1", "2", "3", "4", "5", "6", "7", "8", "9"))
                {
                    e.Handled = true;
                    return;
                }

                if (ShowTrailingZeros && e.Text.In("0", "1", "2", "3", "4", "5", "6", "7", "8", "9")
                    && _textBox.Text.Contains(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                    && _textBox.CaretIndex == _textBox.Text.Length)
                {
                    e.Handled = true;
                    return;
                }
                else
                {
                    _position.Key = CurrentKey.Number;
                }
            }
            finally
            {
                if (!e.Handled)
                {
                    setPositionOffset();
                }
            }
        }

        private void textBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            _gotFocus = false;
            _position.Key = CurrentKey.None;
            _position.Offset = 0;
            _position.Exclude = false;

            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                if (!e.Key.In(Key.Home, Key.End, Key.Left, Key.Right))
                    e.Handled = true;
                return;
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (!e.Key.In(Key.Home, Key.End, Key.A, Key.C, Key.V, Key.X, Key.Z, Key.Y))
                    e.Handled = true;
                return;
            }

            switch (e.Key)
            {
                case Key.Left:
                case Key.Right:
                case Key.Home:
                case Key.End:
                    break;
                case Key.Delete:
                    if ((_textBox.SelectionLength == _textBox.Text.Length) || (_textBox.CaretIndex == 0 && _textBox.Text.Length == 1))
                    {
                        Value = null;
                        e.Handled = true;
                        break;
                    }
                    if (DecimalPlaces > 0)
                    {
                        var decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                        var isNegative = _textBox.Text.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NegativeSign, StringComparison.Ordinal) >= 0;
                        var decimalIndex = _textBox.Text.IndexOf(decimalSeparator, StringComparison.Ordinal);
                        if (_textBox.CaretIndex == decimalIndex || _textBox.CaretIndex == _textBox.Text.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator, StringComparison.Ordinal))
                        {
                            {
                                _textBox.CaretIndex++;
                                e.Handled = true;
                                break;
                            }
                        }
                        else if (_textBox.Text.EndsWith(decimalSeparator))
                        {
                            if (!isNegative && _textBox.CaretIndex == decimalIndex - 1 && _textBox.CaretIndex == 0)
                            {
                                Value = null;
                                e.Handled = true;
                                break;
                            }
                            else if (isNegative && _textBox.CaretIndex == decimalIndex - 1 && _textBox.CaretIndex == 1)
                            {
                                Value = null;
                                e.Handled = true;
                                break;
                            }
                            else if (!isNegative && _textBox.SelectionStart == 0 && _textBox.SelectionLength == decimalIndex)
                            {
                                Value = null;
                                e.Handled = true;
                                break;
                            }
                            else if (isNegative && ((_textBox.SelectionStart == 1 && _textBox.SelectionLength == decimalIndex - 1) || (_textBox.SelectionStart == 0 && _textBox.SelectionLength == decimalIndex)))
                            {
                                Value = null;
                                e.Handled = true;
                                break;
                            }
                        }
                    }
                    break;
                case Key.Back:
                    _position.Key = CurrentKey.Back;
                    if ((_textBox.SelectionLength == _textBox.Text.Length) || (_textBox.CaretIndex == 1 && _textBox.Text.Length == 1))
                    {
                        Value = null;
                        e.Handled = true;
                        break;
                    }
                    if (DecimalPlaces > 0)
                    {
                        var decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                        var isNegative = _textBox.Text.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NegativeSign, StringComparison.Ordinal) >= 0;
                        var decimalIndex = _textBox.Text.IndexOf(decimalSeparator, StringComparison.Ordinal);
                        if (_textBox.CaretIndex == decimalIndex + 1)
                        {
                            _textBox.CaretIndex--;
                            e.Handled = true;
                            break;
                        }
                        else if (_textBox.Text.EndsWith(decimalSeparator))
                        {
                            if (!isNegative && _textBox.CaretIndex == decimalIndex && _textBox.CaretIndex == 1)
                            {
                                Value = null;
                                e.Handled = true;
                                break;
                            }
                            else if (isNegative && _textBox.CaretIndex == decimalIndex && _textBox.CaretIndex == 2)
                            {
                                Value = null;
                                e.Handled = true;
                                break;
                            }
                            else if (!isNegative && _textBox.SelectionStart == 0 && _textBox.SelectionLength == decimalIndex)
                            {
                                Value = null;
                                e.Handled = true;
                                break;
                            }
                            else if (isNegative && ((_textBox.SelectionStart == 1 && _textBox.SelectionLength == decimalIndex - 1) || (_textBox.SelectionStart == 0 && _textBox.SelectionLength == decimalIndex)))
                            {
                                Value = null;
                                e.Handled = true;
                                break;
                            }
                        }
                    }
                    setPositionOffset();
                    break;
                case Key.OemPeriod or Key.Decimal:
                    if ((string.IsNullOrEmpty(_textBox.Text) || _textBox.SelectionLength == _textBox.Text.Length) && DecimalPlaces > 0)
                    {
                        Value = 0.0m;
                        e.Handled = true;
                        _position.Offset = 2;
                        _position.Key = CurrentKey.Decimal;
                        break;
                    }
                    break;
            }
        }

        private void textBox_GotFocus(object sender, RoutedEventArgs e)
        {
            _gotFocus = true;
            _textBox.SelectAll();
        }
        #endregion

        #region Private procedures
        private void setPositionOffset()
        {
            if (!ShowTrailingZeros) return;
            if ((_textBox.Text == CultureInfo.CurrentCulture.NumberFormat.NegativeSign && _position.Key != CurrentKey.Decimal) || _textBox.Text.Length == _textBox.SelectionLength || Value == null)
            {
                _position.Exclude = true;
            }

            if (_textBox.Text == CultureInfo.CurrentCulture.NumberFormat.NegativeSign && _position.Key == CurrentKey.Decimal)
            {
                if (DecimalPlaces > 0)
                {
                    _position.Offset = (int)DecimalPlaces;
                    return;
                }
            }

            var sepPos = _textBox.Text.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);

            _position.Offset = _textBox.Text.Length == _textBox.SelectionLength
                ? _textBox.Text.Length - 1
                : sepPos == -1
                    ? _textBox.Text.Length - (_textBox.CaretIndex + _textBox.SelectionLength)
                    : _textBox.CaretIndex <= sepPos
                        ? _textBox.Text.Length - (_textBox.CaretIndex + _textBox.SelectionLength)
                        : _position.Key == CurrentKey.Number
                            ? _textBox.Text.Length - (_textBox.CaretIndex + _textBox.SelectionLength) - 1
                            : _textBox.Text.Length - (_textBox.CaretIndex + _textBox.SelectionLength) + 1;
        }

        private void cutCommandBinding(object sender, ExecutedRoutedEventArgs e)
        {
            _position.Offset = 0;
            _position.Exclude = false;
            _position.Key = CurrentKey.None;

            if (IsReadOnly)
            {
                e.Handled = true;
                return;
            }

            Clipboard.SetText(_textBox.SelectedText);
            if (_textBox.SelectionLength != _textBox.Text.Length)
                _textBox.Text = _textBox.Text.Substring(0, _textBox.SelectionStart) + _textBox.Text.Substring(_textBox.SelectionStart + _textBox.SelectionLength);
            else
                Value = null;
        }

        private void pasteCommandBinding(object sender, ExecutedRoutedEventArgs e)
        {
            _position.Offset = 0;
            _position.Exclude = false;
            _position.Key = CurrentKey.None;

            if (IsReadOnly)
            {
                e.Handled = true;
                return;
            }

            if (Clipboard.ContainsText())
            {
                var text = Clipboard.GetText();
                if (!decimal.TryParse(text, out _))
                {
                    e.Handled = true;
                }
                else
                {
                    _position.Key = CurrentKey.Number;
                    setPositionOffset();
                    if (_textBox.SelectionLength > 0)
                        _textBox.SelectedText = text;
                    else
                        _textBox.Text = _textBox.Text.Insert(_textBox.CaretIndex, text);
                }
            }
            else
            {
                e.Handled = true;
            }
        }
        #endregion
    }

    internal static class Extensions
    {
        internal static bool In<T>(this T obj, params T[] values) => values.Contains(obj);
    }

    /// <summary>
    /// 
    /// </summary>
    public class NumericBoxForegroundConverter : IMultiValueConverter
    {
        /// <summary>
        /// Determines NumericBox foreground.
        /// </summary>
        /// <param name="values">Array consists of current NumericBox value, regular foreground brush and negative foreground brush</param>
        /// <param name="targetType">Not used.</param>
        /// <param name="parameter">Not used.</param>
        /// <param name="culture">Not used.</param>
        /// <returns>Brush depended on current value sign.</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is not decimal decimalValue || values[1] is not Brush foregroundBrush || values[2] is not Brush negativeBrush) return null;
            return decimalValue >= 0 ? foregroundBrush : negativeBrush;
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="value">Not used.</param>
        /// <param name="targetTypes">Not used.</param>
        /// <param name="parameter">Not used.</param>
        /// <param name="culture">Not used.</param>
        /// <returns>Not used.</returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    /// <summary>
    /// Converts NumericBox value/text to string/decimal
    /// </summary>
    public class NumericBoxTextToValueConverter : IMultiValueConverter
    {
        private string _textValue;

        private string getRealFractionString(decimal value, CultureInfo culture)
        {
            var arr = value.ToString().Split(culture.NumberFormat.NumberDecimalSeparator[0]);
            if (arr.Length == 2)
                return arr[1];
            return null;
        }

        private object[] getDecimalFromString(string stringValue)
        {
            if (double.TryParse(stringValue, out double doubleValue))
            {
                if (doubleValue <= (double)decimal.MaxValue && doubleValue >= (double)decimal.MinValue)
                    return new object[] { decimal.Parse(stringValue, NumberStyles.Any) };
                else if (doubleValue > (double)decimal.MaxValue)
                    return new object[] { decimal.MaxValue };
                else
                    return new object[] { decimal.MinValue };
            }
            return null;
        }

        /// <summary>
        /// Converts decimal value to string.
        /// </summary>
        /// <param name="values">Array consists of current NumericBox value, decimal places and separator using flag.</param>
        /// <param name="targetType">Not used.</param>
        /// <param name="parameter">Not used.</param>
        /// <param name="culture">Not used.</param>
        /// <returns>Formatted string.</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is not decimal decimalValue || values[1] is not uint decimalPlaces || values[2] is not bool useSeparator) return "";

            var addMinus = false;
            var showTrailing = true;
            var isFocused = false;
            var truncate = false;
            if (values.Length > 3 && values[3] is bool bl && !bl)
                showTrailing = false;
            if (values.Length > 4 && values[4] is bool fc)
                isFocused = fc;
            if (values.Length > 5 && values[5] is bool tr)
                truncate = tr;

            var decimalSeparator = culture.NumberFormat.NumberDecimalSeparator;

            if (decimalValue == Constants.EPSILON)
            {
                var text = _textValue;
                if (!showTrailing)
                {
                    var arr = text.Split(decimalSeparator[0]);
                    if (arr.Length == 2 && !string.IsNullOrEmpty(arr[1]) && arr[1].Length >= decimalPlaces)
                    {
                        text = $"{arr[0]}{decimalSeparator}{arr[1].TrimEnd('0')}";
                    }
                    return text;
                }
                else
                {
                    if (text == culture.NumberFormat.NegativeSign)
                    {
                        return text;
                    }
                    else
                    {
                        addMinus = true;
                        decimalValue = 0;
                    }
                }
            }
            else if (decimalValue == -Constants.EPSILON)
                return _textValue;

            var partInt = decimal.Truncate(decimalValue);
            var fractionCount = truncate ? decimalPlaces : BitConverter.GetBytes(decimal.GetBits(decimalValue)[3])[2];

            var partFraction = Math.Abs(decimal.Truncate((decimalValue - partInt) * (int)Math.Pow(10.0, fractionCount)));

            var formatInt = useSeparator ? "#" + culture.NumberFormat.NumberGroupSeparator + "##0" : "##0";
            var formatFraction = new string('0', (int)decimalPlaces);
            var stringInt = partInt.ToString(formatInt);

            string result;
            if (truncate)
            {
                var stringFraction = partFraction.ToString(formatFraction);
                if (!showTrailing && decimalPlaces > 0 && stringFraction.EndsWith("0"))
                {
                    var realDecimalString = getRealFractionString(decimalValue, culture);
                    if (realDecimalString == null || realDecimalString.Length >= decimalPlaces)
                    {
                        while (!stringFraction.EndsWith($"{decimalSeparator}0"))
                        {
                            stringFraction = stringFraction.Substring(0, stringFraction.Length - 1);
                            if (!stringFraction.EndsWith("0"))
                                break;
                        }
                    }
                    else
                    {
                        stringFraction = realDecimalString;
                    }
                }
                if ((decimalValue < 0 && partInt == 0) || addMinus)
                    stringInt = $"{CultureInfo.CurrentCulture.NumberFormat.NegativeSign}{stringInt}";
                result = decimalPlaces > 0
                    ? string.IsNullOrEmpty(stringFraction) && !isFocused ? stringInt : $"{stringInt}{decimalSeparator}{stringFraction}"
                    : stringInt;
            }
            else
            {
                var wholeFraction = partFraction / (decimal)Math.Pow(10, fractionCount);
                var wholeNumber = partInt >= 0 ? partInt + wholeFraction : partInt - wholeFraction;
                var format = $"{formatInt}{decimalSeparator}{formatFraction}";

                result = wholeNumber.ToString(format);
                if (!showTrailing && decimalPlaces > 0 && result.EndsWith("0"))
                {
                    while (!result.EndsWith($"{decimalSeparator}0"))
                    {
                        result = result.Substring(0, result.Length - 1);
                        if (!result.EndsWith("0"))
                            break;
                    }
                }
                if ((decimalValue < 0 && partInt == 0) || addMinus)
                    result = $"{CultureInfo.CurrentCulture.NumberFormat.NegativeSign}{result}";
            }
            result = result.TrimEnd(decimalSeparator[0]);
            return result;
        }

        /// <summary>
        /// Converts string to decimal.
        /// </summary>
        /// <param name="value">String.</param>
        /// <param name="targetTypes">Not used.</param>
        /// <param name="parameter">Not used.</param>
        /// <param name="culture">Not used.</param>
        /// <returns>Decimal.</returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            _textValue = null;
            if (value is not string stringValue) return null;
            if (!string.IsNullOrEmpty(stringValue))
                stringValue = stringValue.Replace(culture.NumberFormat.NumberGroupSeparator, "");
            else
                return null;
            object[] result;
            if (stringValue != culture.NumberFormat.NegativeSign)
            {
                if (stringValue == $"{culture.NumberFormat.NegativeSign}{culture.NumberFormat.NumberDecimalSeparator}")
                {
                    _textValue = stringValue;
                    result = new object[] { -Constants.EPSILON };
                }
                else if (stringValue == culture.NumberFormat.NumberDecimalSeparator)
                {
                    _textValue = stringValue;
                    result = new object[] { -Constants.EPSILON };
                }
                else if (stringValue == $"{culture.NumberFormat.NegativeSign}0")
                {
                    _textValue = stringValue;
                    result = new object[] { -Constants.EPSILON };
                }
                else if (stringValue.EndsWith(culture.NumberFormat.NumberDecimalSeparator))
                {
                    _textValue = stringValue;
                    result = new object[] { Constants.EPSILON };
                }
                else if (stringValue.StartsWith($"{culture.NumberFormat.NegativeSign}0{culture.NumberFormat.NumberDecimalSeparator}"))
                {
                    if (stringValue == $"{culture.NumberFormat.NegativeSign}0{culture.NumberFormat.NumberDecimalSeparator}")
                    {
                        _textValue = stringValue;
                        result = new object[] { Constants.EPSILON };
                    }
                    else
                    {
                        var arr = stringValue.Split(culture.NumberFormat.NumberDecimalSeparator[0]);
                        if (arr.Length == 2 && arr[1].All(c => c == '0'))
                        {
                            _textValue = $"{culture.NumberFormat.NegativeSign}0{culture.NumberFormat.NumberDecimalSeparator}{arr[1]}";
                            result = new object[] { Constants.EPSILON };
                        }
                        else
                        {
                            result = getDecimalFromString(stringValue);
                        }
                    }
                }
                else
                {
                    result = getDecimalFromString(stringValue);
                }
            }
            else
            {
                _textValue = stringValue;
                result = new object[] { Constants.EPSILON };
            }
            return result;
        }
#nullable restore
    }
}
