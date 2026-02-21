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
    private string _status = "Enter student data and click Add Student.";
    private bool _isBusy;

    // Query-mode state: true = Get All; false = Get By Student ID.
    private bool _isGetAllSelected = true;
    private string? _studentIdFilter;

    public StudentEntryViewModel(TcpStudentService service)
    {
        _service = service;
        DisplayName = "Student Entry";
    }

    public string FullName
    {
        get => _fullName;
        set { _fullName = value; NotifyOfPropertyChange(); NotifyOfPropertyChange(nameof(CanAddStudent)); }
    }

    public string StudentId
    {
        get => _studentId;
        set { _studentId = value; NotifyOfPropertyChange(); NotifyOfPropertyChange(nameof(CanAddStudent)); }
    }

    public string Math
    {
        get => _math;
        set { _math = value; NotifyOfPropertyChange(); NotifyOfPropertyChange(nameof(CanAddStudent)); }
    }

    public string Literature
    {
        get => _literature;
        set { _literature = value; NotifyOfPropertyChange(); NotifyOfPropertyChange(nameof(CanAddStudent)); }
    }

    public string English
    {
        get => _english;
        set { _english = value; NotifyOfPropertyChange(); NotifyOfPropertyChange(nameof(CanAddStudent)); }
    }

    public string Status
    {
        get => _status;
        set { _status = value; NotifyOfPropertyChange(); }
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
        }
    }

    /// <summary>Bound to the "Get All" RadioButton.</summary>
    public bool IsGetAllSelected
    {
        get => _isGetAllSelected;
        set
        {
            _isGetAllSelected = value;
            NotifyOfPropertyChange();
            // Keep IsGetByIdSelected in sync (opposite of IsGetAllSelected).
            NotifyOfPropertyChange(nameof(IsGetByIdSelected));
            NotifyOfPropertyChange(nameof(CanGetResults));
        }
    }

    /// <summary>Bound to the "Get By ID" RadioButton; computed as the inverse of IsGetAllSelected.</summary>
    public bool IsGetByIdSelected
    {
        get => !_isGetAllSelected;
        set
        {
            _isGetAllSelected = !value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(IsGetAllSelected));
            NotifyOfPropertyChange(nameof(CanGetResults));
        }
    }

    /// <summary>Student ID filter used when IsGetByIdSelected is true.</summary>
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

    // Bound to the DataGrid in StudentEntryView.
    public ObservableCollection<StudentResultDto> Results { get; } = [];

    public bool CanAddStudent
        => !IsBusy
        && _service.IsDbConnected
        && !string.IsNullOrWhiteSpace(FullName)
        && !string.IsNullOrWhiteSpace(StudentId)
        && TryParseScore(Math, out _)
        && TryParseScore(Literature, out _)
        && TryParseScore(English, out _);

    public bool CanGetResults
        => !IsBusy
        && _service.IsDbConnected
        // When "Get By ID" is selected, a non-empty filter is required.
        && (_isGetAllSelected || !string.IsNullOrWhiteSpace(_studentIdFilter));

    public async Task AddStudent()
    {
        if (!TryParseScore(Math, out double math)
         || !TryParseScore(Literature, out double lit)
         || !TryParseScore(English, out double eng))
        {
            Status = "Scores must be numbers between 0 and 10.";
            return;
        }

        IsBusy = true;
        Status = $"Adding student {StudentId}...";

        try
        {
            var request = new StudentAddRequest(
                FullName: FullName,
                StudentId: StudentId,
                Math: math,
                Literature: lit,
                English: eng);

            var response = await _service.SendStudentAddAsync(request);

            Status = response.Success
                ? $"Student {StudentId} added successfully."
                : $"Failed: {response.ErrorMessage}";

            if (response.Success)
                ClearInputs();
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task GetResults()
    {
        // Build the request according to the selected query mode.
        var request = _isGetAllSelected
            ? new ResultsGetRequest(ResultsMode.All, null)
            : new ResultsGetRequest(ResultsMode.ById, StudentIdFilter);

        IsBusy = true;
        Status = "Fetching results...";

        try
        {
            var rows = await _service.SendResultsGetAsync(request);

            Results.Clear();
            foreach (var row in rows)
                Results.Add(row);

            Status = $"{Results.Count} record(s) loaded.";
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
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

    // Returns false when the value is empty, non-numeric, or outside [0, 10].
    private static bool TryParseScore(string raw, out double value)
    {
        if (double.TryParse(raw,
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
