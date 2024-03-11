using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;

namespace BarberApp_AdminPortal.Shared
{
    public partial class MainLayout : INotifyPropertyChanged
    {
        [Inject] IJSRuntime _jsRuntime { get; set; }


        [Inject]
        protected ILogger<MainLayout> Logger { get; set; }

        [Inject]
        public IServiceScopeFactory serviceScopeFactory { get; set; }

        [Inject]
        public NavigationManager NavigationManager { get; set; }

        [CascadingParameter]
        private Task<AuthenticationState> AuthenticationStateTask
        {
            get
            {
                return _authenticationStateTask;
            }
            set
            {
                _authenticationStateTask = value;
                NotifyPropertyChanged();
            }
        }
        private Task<AuthenticationState> _authenticationStateTask;

        private string title;
        public string Title
        {
            get { return title; }
            set
            {
                title = value;
                StateHasChanged();
            }
        }
        public System.Security.Claims.ClaimsPrincipal User { get; set; }

        //protected override async  Task OnParametersSetAsync()
        //{
        //    User = (await AuthenticationStateTask).User;

        //    if (!(User?.Identity?.IsAuthenticated).GetValueOrDefault())
        //    {
        //        NavigationManager.NavigateTo($"identity/account/login?returnUrl={Uri.EscapeDataString(NavigationManager.Uri)}", true);
        //    } 

        //}

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender)
            {
                await _jsRuntime.InvokeVoidAsync("initializeJs");

            }

        }

        public string Error { get; set; }


        public string UserIp { get; set; }

        public string UserDateTime { get; set; }
        public bool ShowLogoutDialog { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        ///--- logout timer

        private System.Timers.Timer timerObj;

        protected override async Task OnInitializedAsync()
        {
            // Set the Timer delay.
            timerObj = new System.Timers.Timer(900_000); // 15 minutes
            timerObj.Elapsed += UpdateTimer;
            timerObj.AutoReset = false;
            // Identify whether the user is active or inactive using onmousemove and onkeypress in JS function.
            await _jsRuntime.InvokeVoidAsync("timeOutCall", DotNetObjectReference.Create(this));
        }

        [JSInvokable]
        public void TimerInterval()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.ResetColor();
            // Resetting the Timer if the user in active state.
            timerObj.Stop();
            // Call the TimeInterval to logout when the user is inactive.
            timerObj.Start();
        }

        private void UpdateTimer(Object source, ElapsedEventArgs e)
        {
            InvokeAsync(async () => {
                // Log out when the user is inactive.
                var authstate = await AuthenticationStateTask;
                if (authstate.User.Identity.IsAuthenticated)
                {
                    NavigationManager.NavigateTo("identity/account/logout", true);
                }
            });
        }
    }
}

