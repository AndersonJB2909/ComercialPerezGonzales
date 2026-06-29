using System;
using System.Windows.Controls;
using System.Windows.Threading;
using ComercialPerezGonzales.Data.Repositories;
using ComercialPerezGonzales.ViewModels.Base;

namespace ComercialPerezGonzales.ViewModels.Login;

public class LoginViewModel : ViewModelBase
{
    private readonly ConfiguracionRepository _configRepo;
    private readonly Action _onLoginSuccess;
    
    private string _errorMessage = string.Empty;
    private bool _isChangingPassword;
    private string _changePasswordMessage = string.Empty;
    private string _changePasswordMessageColor = "#EF4444";

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsChangingPassword
    {
        get => _isChangingPassword;
        set => SetProperty(ref _isChangingPassword, value);
    }

    public string ChangePasswordMessage
    {
        get => _changePasswordMessage;
        set => SetProperty(ref _changePasswordMessage, value);
    }

    public string ChangePasswordMessageColor
    {
        get => _changePasswordMessageColor;
        set => SetProperty(ref _changePasswordMessageColor, value);
    }

    public RelayCommand LoginCommand { get; }
    public RelayCommand ShowChangePasswordCommand { get; }
    public RelayCommand CancelChangePasswordCommand { get; }
    public RelayCommand ChangePasswordCommand { get; }

    public LoginViewModel(ConfiguracionRepository configRepo, Action onLoginSuccess)
    {
        _configRepo = configRepo;
        _onLoginSuccess = onLoginSuccess;
        
        LoginCommand = new RelayCommand(ExecuteLogin);
        
        ShowChangePasswordCommand = new RelayCommand(() =>
        {
            IsChangingPassword = true;
            ChangePasswordMessage = string.Empty;
            ErrorMessage = string.Empty;
        });

        CancelChangePasswordCommand = new RelayCommand(() =>
        {
            IsChangingPassword = false;
            ChangePasswordMessage = string.Empty;
            ErrorMessage = string.Empty;
        });

        ChangePasswordCommand = new RelayCommand(ExecuteChangePassword);
    }

    private void ExecuteLogin(object? parameter)
    {
        if (parameter is PasswordBox passwordBox)
        {
            string password = passwordBox.Password;
            string? storedHash = _configRepo.GetValor("pos_password");

            if (string.IsNullOrEmpty(storedHash))
                storedHash = SecurityHelper.HashPassword("admin123");

            bool correcto = SecurityHelper.IsHashed(storedHash)
                ? SecurityHelper.HashPassword(password) == storedHash
                : password == storedHash; // fallback por si la migración aún no corrió

            if (correcto)
            {
                ErrorMessage = string.Empty;
                passwordBox.Clear();
                _onLoginSuccess.Invoke();
            }
            else
            {
                ErrorMessage = "Contraseña incorrecta. Inténtelo de nuevo.";
            }
        }
    }

    private void ExecuteChangePassword(object? parameter)
    {
        if (parameter is StackPanel panel)
        {
            PasswordBox? oldPb = null;
            PasswordBox? newPb = null;
            PasswordBox? confirmPb = null;

            foreach (var child in panel.Children)
            {
                if (child is PasswordBox pb)
                {
                    if (pb.Name == "TxtOldPassword") oldPb = pb;
                    else if (pb.Name == "TxtNewPassword") newPb = pb;
                    else if (pb.Name == "TxtConfirmPassword") confirmPb = pb;
                }
            }

            if (oldPb != null && newPb != null && confirmPb != null)
            {
                string oldPass = oldPb.Password;
                string newPass = newPb.Password;
                string confirmPass = confirmPb.Password;

                string? storedHash = _configRepo.GetValor("pos_password");
                if (string.IsNullOrEmpty(storedHash))
                    storedHash = SecurityHelper.HashPassword("admin123");

                bool oldCorrecto = SecurityHelper.IsHashed(storedHash)
                    ? SecurityHelper.HashPassword(oldPass) == storedHash
                    : oldPass == storedHash;

                if (!oldCorrecto)
                {
                    ChangePasswordMessage = "La contraseña anterior es incorrecta.";
                    ChangePasswordMessageColor = "#EF4444";
                    return;
                }

                if (string.IsNullOrWhiteSpace(newPass))
                {
                    ChangePasswordMessage = "La nueva contraseña no puede estar vacía.";
                    ChangePasswordMessageColor = "#EF4444";
                    return;
                }

                if (newPass != confirmPass)
                {
                    ChangePasswordMessage = "Las contraseñas nuevas no coinciden.";
                    ChangePasswordMessageColor = "#EF4444";
                    return;
                }

                // Guardar la nueva contraseña como hash
                _configRepo.SetValor("pos_password", SecurityHelper.HashPassword(newPass));

                ChangePasswordMessage = "¡Contraseña actualizada con éxito!";
                ChangePasswordMessageColor = "#22C55E";

                oldPb.Clear();
                newPb.Clear();
                confirmPb.Clear();

                // Regresar al Login después de 1.5 segundos
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    IsChangingPassword = false;
                    ChangePasswordMessage = string.Empty;
                };
                timer.Start();
            }
        }
    }
}
