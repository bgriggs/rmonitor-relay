using static System.Runtime.InteropServices.JavaScript.JSType;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Hosting;
using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using RedMist.RMonitorRelay.Services;
using System.ComponentModel;
using System.Threading.Tasks;

namespace RedMist.RMonitorRelay.ViewModels;

public partial class MainViewModel : ObservableValidator
{
    private readonly ISettingsProvider settings;

    private string ip = string.Empty;
    [CustomValidation(typeof(MainViewModel), nameof(IpValidate))]
    public string Ip
    {
        get => ip;
        set => SetProperty(ref ip, value, validate: true);
    }

    private int port = 50000;
    [Range(1, 65535)]
    public int Port
    {
        get => port;
        set => SetProperty(ref port, value, validate: true);
    }

    private string clientSecret = string.Empty;
    [StringLength(32, MinimumLength = 5)]
    public string ClientSecret
    {
        get => clientSecret;
        set => SetProperty(ref clientSecret, value);
    }


    public MainViewModel(ISettingsProvider settings)
    {
        this.settings = settings;
    }

    public void LoadSettings()
    {
        Ip = settings.GetWithOverride("RMonitorIP") ?? string.Empty;
        Port = int.TryParse(settings.GetWithOverride("RMonitorPort"), out var port) ? port : 50000;
        ClientSecret = settings.GetWithOverride("RedMistClientSecret") ?? string.Empty;
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(Ip))
        {
            settings.SaveUser("RMonitorIP", Ip);
        }
        else if (e.PropertyName == nameof(Port))
        {
            settings.SaveUser("RMonitorPort", Port.ToString());
        }
        else if (e.PropertyName == nameof(ClientSecret))
        {
            settings.SaveUser("RedMistClientSecret", ClientSecret);
        }
    }


    public static ValidationResult IpValidate(string host, ValidationContext context)
    {
        if (IPAddress.TryParse(host, out _))
        {
            return ValidationResult.Success!;
        }

        return new ValidationResult("IP Address is not valid");
    }
}
