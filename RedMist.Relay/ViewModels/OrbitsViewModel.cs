using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RedMist.Relay.ViewModels;

public partial class OrbitsViewModel : ObservableValidator
{
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

    public static ValidationResult IpValidate(string host, ValidationContext context)
    {
        if (IPAddress.TryParse(host, out _))
        {
            return ValidationResult.Success!;
        }

        return new ValidationResult("IP Address is not valid");
    }
}
