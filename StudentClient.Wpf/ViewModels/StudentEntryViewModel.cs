using Caliburn.Micro;
using Student.Shared.DTOs;
using StudentClient.Wpf.Services;
using System.Collections.ObjectModel;

namespace StudentClient.Wpf.ViewModels;

public sealed class StudentEntryViewModel : Screen
{
    private readonly TcpStudentService _service;

    private string _fullName = string.Empty;
    private string _studentId = string.Empty;
    private string _math = string.Empty;
    private string _literature = string.Empty;
    private string _english = string.Empty;
    private string _status = "Nhập thông tin sinh viên rồi nhấn Lưu sinh viên.";
    private bool _isBusy;

    private bool _isGetAllSelected = true;
    private string? _studentIdFilter;

    public StudentEntryViewModel(TcpStudentService service)
    {
        _service = service;
        _service.StateChanged += OnServiceStateChanged;
        DisplayName = "Dữ liệu sinh viên";
    }

    public string FullName
    {
        get => _fullName;
        set
        {
            _fullName = value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(CanAddStudent));
        }
    }

    public string StudentId
    {
        get => _studentId;
        set
        {
            _studentId = value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(CanAddStudent));
        }
    }

    public string Math
    {
        get => _math;
        set
        {
            _math = value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(CanAddStudent));
        }
    }

    public string Literature
    {
        get => _literature;
        set
        {
            _literature = value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(CanAddStudent));
        }
    }

    public string English
    {
        get => _english;
        set
        {
            _english = value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(CanAddStudent));
        }
    }

    public string Status
    {
        get => _status;
        set
        {
            _status = value;
            NotifyOfPropertyChange();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(CanAddStudent));
            NotifyOfPropertyChange(nameof(CanGetResults));
            NotifyOfPropertyChange(nameof(AreInputsEnabled));
            NotifyOfPropertyChange(nameof(IsStudentIdFilterEnabled));
        }
    }

    public bool AreInputsEnabled => !IsBusy && _service.IsConnected && _service.IsDbConnected;

    public bool IsStudentIdFilterEnabled => AreInputsEnabled && IsGetByIdSelected;

    public bool IsGetAllSelected
    {
        get => _isGetAllSelected;
        set
        {
            _isGetAllSelected = value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(IsGetByIdSelected));
            NotifyOfPropertyChange(nameof(CanGetResults));
            NotifyOfPropertyChange(nameof(IsStudentIdFilterEnabled));
        }
    }

    public bool IsGetByIdSelected
    {
        get => !_isGetAllSelected;
        set
        {
            _isGetAllSelected = !value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(IsGetAllSelected));
            NotifyOfPropertyChange(nameof(CanGetResults));
            NotifyOfPropertyChange(nameof(IsStudentIdFilterEnabled));
        }
    }

    public string? StudentIdFilter
    {
        get => _studentIdFilter;
        set
        {
            _studentIdFilter = value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(CanGetResults));
        }
    }

    public ObservableCollection<StudentResultDto> Results { get; } = [];

    public bool CanAddStudent
        => !IsBusy
        && _service.IsConnected
        && _service.IsDbConnected
        && !string.IsNullOrWhiteSpace(FullName)
        && !string.IsNullOrWhiteSpace(StudentId)
        && TryParseScore(Math, out _)
        && TryParseScore(Literature, out _)
        && TryParseScore(English, out _);

    public bool CanGetResults
        => !IsBusy
        && _service.IsConnected
        && _service.IsDbConnected
        && (_isGetAllSelected || !string.IsNullOrWhiteSpace(_studentIdFilter));

    public async Task AddStudent()
    {
        if (!TryParseScore(Math, out double math)
            || !TryParseScore(Literature, out double literature)
            || !TryParseScore(English, out double english))
        {
            Status = "Điểm phải là số trong khoảng từ 0 đến 10.";
            return;
        }

        IsBusy = true;
        Status = $"Đang lưu sinh viên {StudentId}...";

        try
        {
            var request = new StudentAddRequest(
                FullName: FullName,
                StudentId: StudentId,
                Math: math,
                Literature: literature,
                English: english);

            var response = await _service.SendStudentAddAsync(request);

            if (response.Success)
            {
                Status = $"Đã lưu sinh viên {StudentId} thành công.";
                ClearInputs();
            }
            else
            {
                Status = $"Không thể lưu sinh viên: {response.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            Status = $"Có lỗi khi lưu sinh viên: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task GetResults()
    {
        var request = _isGetAllSelected
            ? new ResultsGetRequest(ResultsMode.All, null)
            : new ResultsGetRequest(ResultsMode.ById, StudentIdFilter);

        IsBusy = true;
        Status = "Đang tải dữ liệu từ máy chủ...";

        try
        {
            var rows = await _service.SendResultsGetAsync(request);

            Results.Clear();
            foreach (var row in rows)
            {
                Results.Add(row);
            }

            Status = Results.Count == 0
                ? "Không tìm thấy dữ liệu phù hợp."
                : $"Đã tải {Results.Count} bản ghi.";
        }
        catch (Exception ex)
        {
            Status = $"Không thể tải dữ liệu: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnServiceStateChanged(object? sender, EventArgs e)
    {
        NotifyOfPropertyChange(nameof(CanAddStudent));
        NotifyOfPropertyChange(nameof(CanGetResults));
        NotifyOfPropertyChange(nameof(AreInputsEnabled));
        NotifyOfPropertyChange(nameof(IsStudentIdFilterEnabled));

        if (!_service.IsConnected)
        {
            Status = "Mất kết nối tới máy chủ. Vui lòng kết nối lại.";
        }
        else if (!_service.IsDbConnected)
        {
            Status = "Chưa có kết nối cơ sở dữ liệu hoạt động.";
        }
    }

    private void ClearInputs()
    {
        FullName = string.Empty;
        StudentId = string.Empty;
        Math = string.Empty;
        Literature = string.Empty;
        English = string.Empty;
    }

    private static bool TryParseScore(string raw, out double value)
    {
        if (double.TryParse(
                raw,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out value))
        {
            return value is >= 0.0 and <= 10.0;
        }

        value = 0;
        return false;
    }
}
