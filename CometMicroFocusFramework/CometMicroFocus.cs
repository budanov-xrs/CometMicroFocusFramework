using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using BR.AN.PviServices;
using Cpu = BR.AN.PviServices.Cpu;
using Service = BR.AN.PviServices.Service;
using Variable = BR.AN.PviServices.Variable;

namespace CometMicroFocusFramework
{
    public class CometMicroFocus : INotifyPropertyChanged
    {
        private const string ServiceName = "service";
        private new const string IpAddress = "192.168.12.10";
        private new const short Port = 11159;

        private const short SourcePort = 26575;
        private const byte SourceStation = 1;

        private Service _service;
        private Cpu _cpu;
        private Variable _variable;

        private enum MicroFocusCommands
        {
            XrayOn,
            XrayOff,
            StartWarmUp,
            AutoCenterKv,
            AutoCenterAll,
            Conditioning,
            FilamentAdjust,
            StartUp,
            TxiOff,
            TxiOn,
            Vacuum,
            IsowattFunctionOn,
            IsowatFunctionOff,
            XrayTimerOn,

            StatusXrayOn,
            Interlock,
            StatusAutocenter,
            StatusDefocusing,
            StatusFilamentAdjust,
            StatusHvGenerator,
            StatusStartUp,
            StatusSystem,
            StatusTxiActive,
            StatusVacuum,
            StatusWarmUp,

            SetKv,
            GetActualKv,
            SetMa,
            GetActualMa,
            SetIsowatt,
            SetTimerDuration,
            SetFocusingCurrent,
            SetModeNumber,
            GetActualModeNumber,
            SetValueFilamentCurrent,
            GetWorkingHours,

            GetSystemError,
            GetSystemErrorQuit,
            HsgError,
            HsgErrorQuit,
            TubeError,
            TubeErrorQuit,
            VacuumError,
            VacuumErrorQuit
        }

        private readonly Dictionary<MicroFocusCommands, string> _commands = new Dictionary<MicroFocusCommands, string>()
        {
            [MicroFocusCommands.AutoCenterKv] = "PC_cmd.autocentkV",
            [MicroFocusCommands.AutoCenterAll] = "PC_cmd.autocentALL",
            [MicroFocusCommands.Conditioning] = "PC_cmd.conditioning",
            [MicroFocusCommands.FilamentAdjust] = "PC_cmd.filamentadjust",
            [MicroFocusCommands.StartUp] = "PC_cmd.startUP",
            [MicroFocusCommands.TxiOff] = "PC_cmd.Ti_reg_off",
            [MicroFocusCommands.TxiOn] = "PC_cmd.Ti_reg_on",
            [MicroFocusCommands.StartWarmUp] = "PC_cmd.warmUP",
            [MicroFocusCommands.XrayOff] = "PC_cmd.xrayOFF",
            [MicroFocusCommands.XrayOn] = "PC_cmd.xrayON",
            [MicroFocusCommands.Vacuum] = "Vacon",
            [MicroFocusCommands.IsowattFunctionOn] = "PC_cmd.isoON",
            [MicroFocusCommands.IsowatFunctionOff] = "PC_cmd.isoOFF",
            [MicroFocusCommands.XrayTimerOn] = "PC_cmd.xraytimerON",

            [MicroFocusCommands.StatusXrayOn] = "PLC_stat.bo_Xray_on",
            [MicroFocusCommands.Interlock] = "Di_Interlock ",
            [MicroFocusCommands.StatusAutocenter] = "PLC_stat.u32_autocenter",
            [MicroFocusCommands.StatusDefocusing] = "PLC_stat.u32_defocusing",
            [MicroFocusCommands.StatusFilamentAdjust] = "PLC_stat.u32_filamentadjust",
            [MicroFocusCommands.StatusHvGenerator] = "PLC_stat.u32_HSG",
            [MicroFocusCommands.StatusStartUp] = "PLC_stat.u32_startup",
            [MicroFocusCommands.StatusSystem] = "PLC_stat.u32_system",
            [MicroFocusCommands.StatusTxiActive] = "PLC_stat.bo_TXI",
            [MicroFocusCommands.StatusVacuum] = "PLC_stat.u32_vacuum",
            [MicroFocusCommands.StatusWarmUp] = "PLC_stat.u32_warmup",

            [MicroFocusCommands.SetKv] = "PC_cmd.r32_TubeVoltage_kV",
            [MicroFocusCommands.GetActualKv] = "PLC_stat.r32_TubeVoltage_kV_ist",
            [MicroFocusCommands.SetMa] = "PC_cmd.r32_TubeCurrent_uA",
            [MicroFocusCommands.GetActualMa] = "PLC_stat.r32_TubeCurrent_uA_ist",
            [MicroFocusCommands.SetIsowatt] = "ISOwatt",
            [MicroFocusCommands.SetTimerDuration] = "PC_cmd.i32_XrayTimer_set_time",
            [MicroFocusCommands.SetFocusingCurrent] = "PC_cmd.r32_focussoll",
            [MicroFocusCommands.SetModeNumber] = "PC_cmd.u32_set_modenumber",
            [MicroFocusCommands.GetActualModeNumber] = "PLC_stat.u32_current_modenumber",
            [MicroFocusCommands.SetValueFilamentCurrent] = "HSG.FILsoll",
            [MicroFocusCommands.GetWorkingHours] = "PLC_stat.u32_workinghour_tube",

            [MicroFocusCommands.GetSystemError] = "Sys_error",
            [MicroFocusCommands.GetSystemErrorQuit] = "Sys_errorquit",
            [MicroFocusCommands.HsgError] = "HSG_error",
            [MicroFocusCommands.HsgErrorQuit] = "HSG_errorquit",
            [MicroFocusCommands.TubeError] = "Tube_error",
            [MicroFocusCommands.TubeErrorQuit] = "Tube_errorquit",
            [MicroFocusCommands.VacuumError] = "TubeVac_error",
            [MicroFocusCommands.VacuumErrorQuit] = "Vac_errorquit",
        };

        #region Properties

        #region TargetKv : double - Заданное значение напряжения

        private double _targetKv;

        /// <summary> Заданное значение напряжения </summary>
        public double TargetKv
        {
            get => _targetKv;
            set
            {
                _targetKv = value;
                OnPropertyChanged();
            }
        }

        #endregion TargetKv

        #region ActualKv : double - Актуальное значение напряжения

        private double _actualKv;

        /// <summary> Актуальное значение напряжения </summary>
        public double ActualKv
        {
            get => _actualKv;
            protected set
            {
                _actualKv = value;
                OnPropertyChanged();
            }
        }

        #endregion ActualKv

        #region TargetMa : double - Заданное значение тока

        private double _targetMa;

        /// <summary> Заданное значение тока </summary>
        public double TargetMa
        {
            get => _targetMa;
            set
            {
                _targetMa = value;
                OnPropertyChanged();
            }
        }

        #endregion TargetMa

        #region ActualMa : double - Актуальное значение тока

        private double _actualMa;

        /// <summary> Актуальное значение тока </summary>
        public double ActualMa
        {
            get => _actualMa;
            protected set
            {
                _actualMa = value;
                OnPropertyChanged();
            }
        }

        #endregion ActualMa

        #region MinKv : double - Минимальное напряжение для установки

        private double _minKv;

        /// <summary> Минимальное напряжение для установки </summary>
        public double MinKv
        {
            get => _minKv;
            set
            {
                _minKv = value;
                OnPropertyChanged();
            }
        }

        #endregion MinKv

        #region MaxKv : double - Максимальное напряжение для установки

        private double _maxKv;

        /// <summary> Максимальное напряжение для установки </summary>
        public double MaxKv
        {
            get => _maxKv;
            set
            {
                _maxKv = value;
                OnPropertyChanged();
            }
        }

        #endregion MaxKv

        #region MinMa : double - Минимальный ток для установки

        private double _minMa;

        /// <summary> Минимальный ток для установки </summary>
        public double MinMa
        {
            get => _minMa;
            set
            {
                _minMa = value;
                OnPropertyChanged();
            }
        }

        #endregion MinMa

        #region MaxMa : double - Максимальный ток для установки

        private double _maxMa;

        /// <summary> Максимальный ток для установки </summary>
        public double MaxMa
        {
            get => _maxMa;
            protected set
            {
                _maxMa = value;
                OnPropertyChanged();
            }
        }

        #endregion MaxMa

        #region FocalSpot : FocalSpots - Фокусное пятно

        private FocalSpots _focalSpot;

        /// <summary> Фокусное пятно </summary>
        public FocalSpots FocalSpot
        {
            get => _focalSpot;
            set
            {
                _focalSpot = value;
                OnPropertyChanged();
            }
        }

        #endregion FocalSpot

        #region IsEnabled : bool - Статус включения рентгена. False - рентген выключен, True - рентген включен

        private bool _isEnabled;

        /// <summary> Статус включения рентгена. False - рентген выключен, True - рентген включен </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged();
            }
        }

        #endregion IsEnabled

        #region MustQuit : bool - Переменная для отключения цикла

        private bool _mustQuit;

        /// <summary> Переменная для отключения цикла </summary>
        public bool MustQuit
        {
            get => _mustQuit;
            set
            {
                _mustQuit = value;
                OnPropertyChanged();
            }
        }

        #endregion MustQuit

        #region IsConnected : bool - Указывает, есть соединение или нет

        private bool _isConnected;

        /// <summary> Указывает, есть соединение или нет </summary>
        public bool IsConnected
        {
            get => _isConnected;
            protected set
            {
                _isConnected = value;
                OnPropertyChanged();
            }
        }

        #endregion IsConnected

        #region KvStep : double - Шаг смены напряжения

        private double _kvStep;

        /// <summary> Шаг смены напряжения </summary>
        public double KvStep
        {
            get => _kvStep;
            protected set
            {
                _kvStep = value;
                OnPropertyChanged();
            }
        }

        #endregion KvStep

        #region MaStep : double - Шаг смены тока

        private double _maStep;

        /// <summary> Шаг смены тока </summary>
        public double MaStep
        {
            get => _maStep;
            protected set
            {
                _maStep = value;
                OnPropertyChanged();
            }
        }

        #endregion MaStep

        #region MaximalPower : double - Максимально допустимая мощность

        private double _maximalPower;

        /// <summary> Максимально допустимая мощность </summary>
        public double MaximalPower
        {
            get => _maximalPower;
            protected set
            {
                _maximalPower = value;
                OnPropertyChanged();
            }
        }

        #endregion MaximalPower

        #region IsHv : bool - Отображает состояние рентгена Вкл/Выкл Высокое напряжение

        private bool _isHv;

        /// <summary> Отображает состояние рентгена Вкл/Выкл Высокое напряжение </summary>
        public bool IsHv
        {
            get => _isHv;
            protected set
            {
                _isHv = value;
                OnPropertyChanged();
            }
        }

        public bool HasError { get; }
        public TimeSpan CountdownWarmUp { get; }
        public XRayWarmUp WarmUpType { get; }
        public XRayProgram State { get; }
        public virtual event EventHandler<XRayErrors> CriticalErrorMessage;

        #endregion IsHv

        #region IsHv : bool - Отображает состояние рентгена Вкл/Выкл Высокое напряжение

        private bool _isReady;

        /// <summary> Отображает состояние рентгена Вкл/Выкл Высокое напряжение </summary>
        public bool IsReady
        {
            get => _isReady;
            protected set
            {
                _isReady = value;
                OnPropertyChanged();
            }
        }

        #endregion IsHv

        #region NeedWarmUp : bool - Указывает, требуется тренировка

        private bool _needWarmUp;

        /// <summary> Указывает, требуется тренировка </summary>
        public bool NeedWarmUp
        {
            get => _needWarmUp;
            protected set => SetField(ref _needWarmUp, value);
        }

        #endregion NeedWarmUp

        #region KvResolution : double - Шаг напряжения

        private double _kvResolution;

        /// <summary> Шаг напряжения </summary>
        public double KvResolution
        {
            get => _kvResolution;
            set => SetField(ref _kvResolution, value);
        }

        #endregion KvResolution

        #region MaResolution : double - Шаг тока

        private double _maResolution;

        /// <summary> Шаг тока </summary>
        public double MaResolution
        {
            get => _maResolution;
            set => SetField(ref _maResolution, value);
        }

        #endregion MaResolution

        public double Vacuum { get; set; }

        #endregion Properties


        public CometMicroFocus()
        {
            if (_service == null)
            {
                _service = new Service(ServiceName);
                _service.Connected += ServiceConnected;
                _service.Error += ServiceError;
            }

            _service.Connect();
        }

        #region Private Methods

        /// <summary>
        /// Connect CPU object if service object connection successful
        ///</summary>
        private void ServiceConnected(object sender, PviEventArgs e)
        {
            // log.logMsg(Logger.logLevelT.INFO, "Service connected");
            if (_cpu == null)
            {
                // Create CPU object and add the event handler 
                _cpu = new Cpu(_service, "cpu");
                _cpu.Connected += CpuConnected;
                _cpu.DateTimeRead += CpuDateTimeRead;
                // Set the connection properties for a TCP/IP connection 
                _cpu.Connection.DeviceType = DeviceType.TcpIp;
                _cpu.Connection.TcpIp.DestinationIpAddress = IpAddress;
                _cpu.Connection.TcpIp.DestinationPort = Port;
                //if the Destination Station is not specified, it looks like the system automatically determines it.
                //cpu.Connection.TcpIp.DestinationStation = Properties.Settings.Default.destination_station;
                _cpu.Connection.TcpIp.SourcePort = SourcePort;
                _cpu.Connection.TcpIp.SourceStation = SourceStation;
            }

            // Connect CPU
            _cpu.Connect();
            
            new Thread(Update).Start();
        }

        /// <summary> 
        /// Handles service connection errors 
        ///</summary>
        private void ServiceError(object sender, PviEventArgs e)
        {
            int errorCode = _service.ErrorCode;
            if (errorCode != 0)
            {
                // log.logMsg(Logger.logLevelT.ERROR, String.Format("Service connection error (error code: {0})!", errorCode));
                // MessageBox.Show(_service.GetErrorText(_service.ErrorCode));
            }
        }

        /// <summary> 
        /// Output text when connection to CPU successful and   
        /// enable Variable Connect button. Additionnaly reads the PLC date and time.
        /// </summary> 
        private void CpuConnected(object sender, PviEventArgs e)
        {
            // log.logMsg(Logger.logLevelT.INFO, string.Format("{0} connected", ((Cpu)sender).Name));
            _cpu?.ReadDateTime();
        }

        /// <summary>
        /// Output the PLC date and time in the log
        /// </summary>
        private void CpuDateTimeRead(object sender, CpuEventArgs e)
        {
            // log.logMsg(Logger.logLevelT.INFO, e.DateTime.ToString());
        }

        private void ConnectVariable(string variableName)
        {
            // Create new (global) variable object -> global
            // variable "count" must be on the controller and should
            // cyclically count up
            // log.logMsg(Logger.logLevelT.INFO, String.Format("Connection to {0} variable requested", tbVarNameRead.Text));
            _variable = new Variable(_cpu, variableName);
            // Activate and connect variable object
            _variable.Active = true;
            _variable.Connect();
            // Add event handler for value changes
            _variable.ValueChanged += VariableValueChanged;
            _variable.ValueWritten += VariableValueWritten;
        }

        /// <summary> 
        /// Output value changes in status field
        /// </summary> 
        private void VariableValueChanged(object sender, VariableEventArgs e)
        {
            var tmpVariable = (Variable)sender;
            var value = tmpVariable.Value.ToString(new CultureInfo("RU-ru"));
            // log.logMsg(Logger.logLevelT.INFO, tmpVariable.Name + ": " + tmpVariable.Value.ToString());
        }

        private void VariableDisconnected(object sender, PviEventArgs e)
        {
            // log.logMsg(Logger.logLevelT.INFO, "Variable disconnected");
            _variable.Dispose();
        }

        private void VariableValueWritten(object sender, PviEventArgs e)
        {
            // log.logMsg(Logger.logLevelT.INFO, "Variable written");
        }

        private void VariableDisconnect()
        {
            // log.logMsg(Logger.logLevelT.INFO, "Variable disconnection requested");
            _variable.Active = false;
            // Add event handler for value changes
            _variable.Disconnected += VariableDisconnected;
            _variable.Disconnect();
        }

        private void VariableWrite(Value value)
        {
            // log.logMsg(Logger.logLevelT.INFO, "Variable write requested");
            _variable.Value = value;
            _variable.WriteValue();
        }

        private void VariableWrite(string variableName, Value value)
        {
            ConnectVariable(variableName);
            VariableWrite(value);
            VariableDisconnect();
        }

        private void VariableRead(string variableName, out Value value)
        {
            ConnectVariable(variableName);
            // READ Variable
            value = _variable.Value;
            VariableDisconnect();
        }

        #endregion Private Methods

        #region Public Methods

        public void SetKv(double value)
        {
            VariableWrite(_commands[MicroFocusCommands.SetKv], value);
        }

        public void SetMa(double value)
        {
            VariableWrite(_commands[MicroFocusCommands.SetMa], value);
        }

        public void TryConnection()
        {
            throw new NotImplementedException();
        }

        public void SetFocalSpot(FocalSpots value)
        {
            throw new NotImplementedException();
        }

        protected void GetMinimalKv()
        {
            throw new NotImplementedException();
        }

        protected void GetMaximalKv()
        {
            throw new NotImplementedException();
        }

        protected void GetActualKv()
        {
            VariableRead(_commands[MicroFocusCommands.GetActualKv], out var value);
            ActualKv = value;
        }

        protected void GetActualMa()
        {
            VariableRead(_commands[MicroFocusCommands.GetActualMa], out var value);
            ActualMa = value;
        }

        protected void GetHvStatus()
        {
            VariableRead(_commands[MicroFocusCommands.StatusXrayOn], out var value);
            IsHv = value;
        }

        public bool On()
        {
            VariableWrite(_commands[MicroFocusCommands.XrayOn], true);
            return true;
        }

        public bool Off()
        {
            VariableWrite(_commands[MicroFocusCommands.XrayOff], true);
            return true;
        }

        public void Disconnect()
        {
            throw new NotImplementedException();
        }

        public bool SetWarmUp(XRayWarmUp warmUp)
        {
            throw new NotImplementedException();
        }

        public void StartWarmUp()
        {
            VariableWrite(_commands[MicroFocusCommands.StartWarmUp], true);
        }

        #endregion Public Methods

        protected void Update()
        {
            Thread.Sleep(500);
            GetActualMa();
            GetActualKv();
        }

        protected void Connect()
        {
            throw new NotImplementedException();
        }

        public void AutoCentering()
        {
            VariableWrite(_commands[MicroFocusCommands.AutoCenterAll], true);
        }

        public void GetVacuum()
        {
            VariableRead(_commands[MicroFocusCommands.Vacuum], out var value);
            Vacuum = value;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public enum FocalSpots
    {
        Large = 0,
        Small = 1,
        Broad = 'B',
        Error = 'E',
        Fine = 'F',
        Unknown = 'U',
    }

    public enum XRayWarmUp
    {
        Disable = 0,
        Short = 1,
        Medium = 2,
        Long = 3,
    }
    
    public enum XRayProgram
    {
        /// <summary>
        /// Ошибка
        /// </summary>
        Error = -1,
        /// <summary>
        /// Готов
        /// </summary>
        Ready = 0,
        /// <summary>
        /// Высокое напряжение
        /// </summary>
        HV = 1,
        /// <summary>
        /// Выключен
        /// </summary>
        Off = 2,
        /// <summary>
        /// Требуется тренировка
        /// </summary>
        NeedWarmUp = 3,
        /// <summary>
        /// В тренировке
        /// </summary>
        WarmUp = 4,
        /// <summary>
        /// Длинная тренировка
        /// </summary>
        WarmupLong,
        /// <summary>
        /// Короткая тренировка
        /// </summary>
        WarmupShort,
        /// <summary>
        /// Аналогично с HV = 1
        /// </summary>
        Fluoro = 800,
        /// <summary>
        /// Установление значение происходит с пульта
        /// </summary>
        Manual = 4,
        /// <summary>
        /// Получение времени наработки (Gulmay)
        /// </summary>
        GetTimeWork = 913,
    }
    
    public enum XRayErrors
    {
        Ok = 0,
        Internal = 1,
        KvValueHighMax = 2,
        KvValueBelowMin = 3,
        MaValueHighMax = 4,
        MaValueBelowMin = 5,
        CurrentTooHigh = 6,
        CurrentTooLow = 7,
        VoltageTooHigh = 8,
        VoltageTooLow = 9,
        Overheating = 10,
        CurrentFanExceeded = 11,
        EmergencyStop = 12
    }
}