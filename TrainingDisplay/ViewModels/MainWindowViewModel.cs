using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using Serilog;
using Training.BusinessLogic.Einstellungen;
using Training.BusinessLogic.Gebucht;
using Training.BusinessLogic.Spielstaetten;
using Training.BusinessLogic.UOW;

namespace TrainingDisplay.ViewModels;

public class MainWindowViewModel : ViewModelBase, INotifyPropertyChanged
{
    public new event PropertyChangedEventHandler? PropertyChanged;
    
    private List<Einstellungen>? _einstellungen;
    
    private List<Anzeige> _pagedItems = [];
    private List<Anzeige> _items = [];
    public List<Anzeige> PagedItems
    {
        get => _pagedItems;
        set
        {
            _pagedItems = value;
            OnPropertyChanged(nameof(PagedItems));
        }
    }
    public string Greeting { get; set; } = string.Empty;

    public bool ShowKuerklasse { get; set; }
    private int ListenIntervall { get; set; }
    private double Trainingsverbot { get; set; } = -50;
    private double ErhoehterTarif { get; set; } = -20;
    private double NegativesGuthaben { get; set; } = -5;
    
    private readonly DispatcherTimer? _timer;
    
    private int _currentPage = 1;
    private int _pageSize = 5;
    
    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            _currentPage = value;
            OnPropertyChanged(nameof(CurrentPage));
            Task.Run(async () =>
            {
                await UpdatePagedItemsAsync();
            });
        }
    }

    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (_pageSize == value)
            {
                return;
            }

            _pageSize = value;
            OnPropertyChanged(nameof(PageSize));
            Task.Run(async () =>
            {
                await UpdatePagedItemsAsync();
            });
        }
    }
    
    public MainWindowViewModel()
    {
        try
        {
            UOW.XPOTrainingConnectionString = Program.ConnectionString;

            Task.Run(
                async () =>
                {
                    _einstellungen = await GetSettingsAsync();
                    await GetLocationAsync();
                    await UpdatePagedItemsAsync();
                }).GetAwaiter().GetResult();
            
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(ListenIntervall)
            };
            _timer.Tick += OnTimerTick;
            _timer.Start();
        }
        catch (Exception e)
        {
            Log.Fatal(e,
                "{@Hostname} {@Application} {@User} {@Action} {@ExtendedInfo} {@Class} {@Method} {@Location} {@Value1} {@Value2} {@Value3} {@Value4} {@Value5}",
                Environment.MachineName, Program.AssemblyName, Environment.UserName, e.Message, e.StackTrace, "MainWindowViewModel",
                "Constructor", Program.Location.ToString(), "", "", "", "", "");
        }
    }
    
    private new void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    private void OnTimerTick(object? sender, EventArgs e)
    {
        try
        {
            _timer?.Stop();

            if (CurrentPage < (_items.Count / PageSize))
            {
                CurrentPage++;
            }
            else
            {
                if (_items.Count % PageSize >= 0 && CurrentPage * PageSize < _items.Count)
                    CurrentPage++;
                else
                    CurrentPage = 1;
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex,
                "{@Hostname} {@Application} {@User} {@Action} {@ExtendedInfo} {@Class} {@Method} {@Location} {@Value1} {@Value2} {@Value3} {@Value4} {@Value5}",
                Environment.MachineName, Program.AssemblyName, Environment.UserName, ex.Message, ex.StackTrace, "MainWindowViewModel",
                "OnTimerTick", Program.Location.ToString(), "", "", "", "", "");
        }
        finally
        {
            _timer?.Start();
        }
    }
    
    private async Task UpdatePagedItemsAsync()
    {
        try
        {
            _items = await GetItemsAsync();
            PagedItems = _items.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
        }
        catch (Exception e)
        {
            Log.Fatal(e,
                "{@Hostname} {@Application} {@User} {@Action} {@ExtendedInfo} {@Class} {@Method} {@Location} {@Value1} {@Value2} {@Value3} {@Value4} {@Value5}",
                Environment.MachineName, Program.AssemblyName, Environment.UserName, e.Message, e.StackTrace, "MainWindowViewModel",
                "UpdatePageItemsAsync", Program.Location.ToString(), "", "", "", "", "");        }
    }
    
    private async Task GetLocationAsync()
    {
        try
        {
            var spielstaette = await Spielstaetten.GetByIdAsync(Program.Location);
            var location = spielstaette.Bezeichnung;
        
            if (!string.IsNullOrWhiteSpace(location))
            {
                Greeting = location.ToLower().Contains("eisring") ? "Eisring Süd begrüßt heute ..." : "Die Eisstadthalle begrüßt heute ...";
            }
            else
            {
                Greeting = "Wir begrüßen heute ...";
            }
        }
        catch (Exception e)
        {
            Log.Fatal(e,
                "{@Hostname} {@Application} {@User} {@Action} {@ExtendedInfo} {@Class} {@Method} {@Location} {@Value1} {@Value2} {@Value3} {@Value4} {@Value5}",
                Environment.MachineName, Program.AssemblyName, Environment.UserName, e.Message, e.StackTrace, "MainWindowViewModel",
                "GetLocatinAsync", Program.Location.ToString(), "", "", "", "", "");
        }
    }
    
    private async Task<List<Einstellungen>?> GetSettingsAsync()
    {
        try
        {
            _einstellungen = await Einstellungen.GetAsync();
            var kuerklasse = _einstellungen.Where(x => x.Setting == "ShowKuerklasse").Select(x => x.Value).FirstOrDefault();
            ShowKuerklasse = kuerklasse != "0";
            PageSize = Convert.ToInt32(_einstellungen.Where(x => x.Setting == "Listenanzahl").Select(x => x.Value).FirstOrDefault());
            ListenIntervall = Convert.ToInt32(_einstellungen.Where(x => x.Setting == "Listenintervall").Select(x => x.Value).FirstOrDefault());
            Trainingsverbot = Convert.ToDouble(_einstellungen.Where(x => x.Setting == "Trainingsverbot").Select(x => x.Value).FirstOrDefault());
            ErhoehterTarif = Convert.ToDouble(_einstellungen.Where(x => x.Setting == "UmstellungAbbuchung").Select(x => x.Value).FirstOrDefault());
            NegativesGuthaben = Convert.ToDouble(_einstellungen.Where(x => x.Setting == "AnzeigeGesperrt").Select(x => x.Value).FirstOrDefault());
        
            return _einstellungen;
        }
        catch (Exception e)
        {
            Log.Fatal(e,
                "{@Hostname} {@Application} {@User} {@Action} {@ExtendedInfo} {@Class} {@Method} {@Location} {@Value1} {@Value2} {@Value3} {@Value4} {@Value5}",
                Environment.MachineName, Program.AssemblyName, Environment.UserName, e.Message, e.StackTrace, "MainWindowViewModel",
                "GetSettingsAsync", Program.Location.ToString(), "", "", "", "", "");
            return null;
        }
    }

    private async Task<List<Anzeige>> GetItemsAsync()
    {
        try
        {
            var anzeige = await Gebucht.GetForMonitorAnzeigeAsync(Program.Location, DateTime.Now, Trainingsverbot,
                ErhoehterTarif, NegativesGuthaben);

            if (Program.LogLevel != null && Program.LogLevel.Equals("debug", StringComparison.CurrentCultureIgnoreCase))
            {
                Log.Debug(
                    "{@Hostname} {@Application} {@User} {@Action} {@ExtendedInfo} {@Class} {@Method} {@Location} {@Value1} {@Value2} {@Value3} {@Value4} {@Value5}",
                    Environment.MachineName, Program.AssemblyName, Environment.UserName, $"{anzeige.Count} items received" , anzeige, "MainWindowViewModel",
                    "GetItemsAsync", Program.Location.ToString(), "", "", "", "", "");
            }
            return anzeige;
        }
        catch (Exception e)
        {
            Log.Fatal(e,
                "{@Hostname} {@Application} {@User} {@Action} {@ExtendedInfo} {@Class} {@Method} {@Location} {@Value1} {@Value2} {@Value3} {@Value4} {@Value5}",
                Environment.MachineName, Program.AssemblyName, Environment.UserName, e.Message, e.StackTrace, "MainWindowViewModel",
                "GetItemsAsync", Program.Location.ToString(), "", "", "", "", "");
            return [];
        }
    }
}