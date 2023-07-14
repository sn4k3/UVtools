using UVtools.Core.Operations;

namespace UVtools.UI.Controls.Tools;

public partial class ToolInfillControl : ToolControl
{
    private sbyte _sbyteNum;
    private byte _byteNum;
    private short _shortNum;
    private ushort _ushortNum;
    private int _intNum;
    private uint _uintNum;
    private long _longNum;
    private ulong _ulongNum;
    private nint _nintNum;
    private nuint _nuintNum;
    private float _floatNum;
    private double _doubleNum;
    private decimal _decimalNum;
    public OperationInfill Operation => BaseOperation as OperationInfill;

    public sbyte sbyteNum
    {
        get => _sbyteNum;
        set
        {
            if (value == _sbyteNum) return;
            _sbyteNum = value;
            RaisePropertyChanged();
        }
    }

    public byte byteNum
    {
        get => _byteNum;
        set
        {
            if (value == _byteNum) return;
            _byteNum = value;
            RaisePropertyChanged();
        }
    }

    public short shortNum
    {
        get => _shortNum;
        set
        {
            if (value == _shortNum) return;
            _shortNum = value;
            RaisePropertyChanged();
        }
    }

    public ushort ushortNum
    {
        get => _ushortNum;
        set
        {
            if (value == _ushortNum) return;
            _ushortNum = value;
            RaisePropertyChanged();
        }
    }

    public int intNum
    {
        get => _intNum;
        set
        {
            if (value == _intNum) return;
            _intNum = value;
            RaisePropertyChanged();
        }
    }

    public uint uintNum
    {
        get => _uintNum;
        set
        {
            if (value == _uintNum) return;
            _uintNum = value;
            RaisePropertyChanged();
        }
    }

    public long longNum
    {
        get => _longNum;
        set
        {
            if (value == _longNum) return;
            _longNum = value;
            RaisePropertyChanged();
        }
    }

    public ulong ulongNum
    {
        get => _ulongNum;
        set
        {
            if (value == _ulongNum) return;
            _ulongNum = value;
            RaisePropertyChanged();
        }
    }

    public nint nintNum
    {
        get => _nintNum;
        set
        {
            if (value.Equals(_nintNum)) return;
            _nintNum = value;
            RaisePropertyChanged();
        }
    }

    public nuint nuintNum
    {
        get => _nuintNum;
        set
        {
            if (value.Equals(_nuintNum)) return;
            _nuintNum = value;
            RaisePropertyChanged();
        }
    }

    public float floatNum
    {
        get => _floatNum;
        set
        {
            if (value.Equals(_floatNum)) return;
            _floatNum = value;
            RaisePropertyChanged();
        }
    }

    public double doubleNum
    {
        get => _doubleNum;
        set
        {
            if (value.Equals(_doubleNum)) return;
            _doubleNum = value;
            RaisePropertyChanged();
        }
    }

    public decimal decimalNum
    {
        get => _decimalNum;
        set
        {
            if (value == _decimalNum) return;
            _decimalNum = value;
            RaisePropertyChanged();
        }
    }

    public ToolInfillControl()
    {
        BaseOperation = new OperationInfill(SlicerFile);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }
}