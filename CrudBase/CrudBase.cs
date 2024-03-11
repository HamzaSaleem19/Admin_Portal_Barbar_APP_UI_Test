using BarberApp_AdminPortal.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Claims;

namespace BarberApp_AdminPortal.CrudBase
{
    public abstract class CrudBase<ComposeViewModel, ListModel, TKey> : ComponentBase, INotifyPropertyChanged
     where ComposeViewModel : class, new()
     where ListModel : class, new()
    {
        [Inject]
        protected IJSRuntime JSRuntime { get; set; }

        private int? _eventId;
        [CascadingParameter]
        public int? EventId
        {
            get { return _eventId; }
            set
            {
                if (_eventId != value)
                {
                    _eventId = value;
                    NotifyPropertyChanged();
                }
            }
        }

        #region Index Properties

        public int TotalPages { get; set; }


        private string _sortColumn;

        public string SortColumn
        {
            get { return _sortColumn; }
            set
            {
                _sortColumn = value;
                NotifyPropertyChanged();
            }
        }
        [Inject]
        protected ILogger<ComposeViewModel> Logger { get; set; }

        private bool _isTableBusy;
        public bool IsTableBusy
        {
            get
            {
                return _isTableBusy;
            }
            set
            {
                _isTableBusy = value;
                PubSub.Hub.Default.Publish(value);
            }
        }
        public int TotalRows { get; set; }

        #endregion

        protected string ModalCss { get; set; }

        protected string BackDrop { get; set; }
        public bool IsModalBusy { get; set; }
        protected string ButtonClasses { get; set; }
        public string ValidationError { get; set; }

        //protected ComposeViewModel SelectedItem { get; private set; }
        private ComposeViewModel _selectedItem;
        public ComposeViewModel SelectedItem
        {
            get
            {
                return _selectedItem;
            }
            set
            {
                _selectedItem = value;
                NotifyPropertyChanged();
            }
        }

        private List<ListModel> _items;
        protected List<ListModel> Items
        {
            get
            {
                return _items;
            }
            set
            {
                _items = value;
                NotifyPropertyChanged();
            }
        }

        protected virtual PaginationStripModel PaginationStrip { get; set; }
        protected string Error { get; set; }

        [CascadingParameter]
        public ClaimsPrincipal User { get; set; }

        public string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        public virtual int RowsPerPage => 12;
        public int SNo;
        public virtual bool UsePagination => true;

        public CrudBase()
        {
            PaginationStrip = new PaginationStripModel()
            {
                CurrentIndex = 1,
                RowsPerPage = RowsPerPage
            };

            PaginationStrip.PropertyChanged += async (p, q) =>
            {
                if (q.PropertyName == nameof(PaginationStrip.RowsPerPage)
                || q.PropertyName == nameof(PaginationStrip.CurrentIndex)
                || q.PropertyName == nameof(SortColumn))
                {
                    await LoadItems(true);
                    if (PaginationStrip.CurrentIndex < 0)
                    {
                        PaginationStrip.CurrentIndex = 1;
                    }
                    else if (TotalPages > 0 && PaginationStrip.CurrentIndex > TotalPages)
                    {
                        PaginationStrip.CurrentIndex = TotalPages;
                    }
                    StateHasChanged();
                }
            };

            PropertyChanged += async (p, q) =>
            {
                if (q.PropertyName == nameof(SortColumn) || q.PropertyName == nameof(EventId))
                {
                    await LoadItems(true);
                    if (q.PropertyName == nameof(EventId))
                    {
                        await LoadSelectLists();
                    }
                }
            };
        }

        protected override Task OnInitializedAsync()
        {
            PubSub.Hub.Default.Subscribe<string>(async (x) =>
            {
                if (x == "Refresh")
                {
                    await LoadItems(false);
                }
            });

            return base.OnInitializedAsync();
        }

        protected async Task BlockFormUI()
        {
            IsModalBusy = true;
            await InvokeAsync(() => StateHasChanged());
            await Task.Delay(1);
        }

        protected async Task UnBlockFormUI()
        {
            IsModalBusy = false;
            await InvokeAsync(() => StateHasChanged());
        }
        protected async Task BlockTableUI()
        {
            IsTableBusy = true;
            await InvokeAsync(() => StateHasChanged());
            await Task.Delay(10);
        }
        protected async Task UnBlockTableUI()
        {
            IsTableBusy = false;
            await InvokeAsync(() => StateHasChanged());
            await Task.Delay(10);
        }

        protected abstract Task LoadSelectLists();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender)
            {

                await LoadSelectLists();
                await LoadItems();
                await InvokeAsync(() => StateHasChanged());
            }
        }
        protected Action OnSelectedItemCreated;
        protected async Task OnItemClicked(TKey id)
        {
            await BlockFormUI();
            Error = null;
            ValidationError = null;
            ShowModal();
            if (id?.ToString() == "0" ||id?.ToString() == Guid.Empty.ToString() || string.IsNullOrEmpty(id?.ToString()))
            {
                SelectedItem = CreateSelectedItem();
            }
            else
            {
                SelectedItem = await GetAsync(id);
            }
            await ItemLoaded();
            OnSelectedItemCreated?.Invoke();
            await UnBlockFormUI();
        }

        /// <summary>
        /// Created SelectedItem with default values.
        /// Override this method if want to create and enforce specific rules on ComposeViewModel,
        /// </summary> 
        protected virtual ComposeViewModel CreateSelectedItem()
        {
            return new ComposeViewModel();
        }

        protected abstract Task<ComposeViewModel> GetAsync(TKey id);

        //protected abstract void RemoveSelectedItem();

        protected Action OnFormSubmitted;
        protected async Task OnFormSubmit()
        {
            await BlockFormUI();

            try
            {
                ButtonClasses = "btn btn-primary pull-right kt-spinner kt-spinner--right kt-spinner--sm kt-spinner--light";
                await PostAsync();

                await LoadItems();
                HideModal();
                SelectedItem = null;
                Error = null;
                ValidationError = null;
                await ItemPersisted();
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                ValidationError = ex.Message;
            }
            OnFormSubmitted?.Invoke();
            await UnBlockFormUI();
        }
        protected virtual async Task ItemsLoaded() { await Task.CompletedTask; }
        protected virtual async Task ItemLoaded() { await Task.CompletedTask; }
        protected virtual async Task ItemPersisted() { await Task.CompletedTask; }

        protected async Task LoadItems(bool showLoader = true)
        {

            if (showLoader) await BlockTableUI();

            try
            {
                SNo = 0;
                ValidationError = null;
                Error = null;
                var query = GetPageAsync();

                if (!string.IsNullOrEmpty(SortColumn))
                {
                    //  query = query.OrderBy(SortColumn); 
                }
                if (UsePagination)
                {
                    TotalRows = query.Count();

                    TotalPages = Convert.ToInt32(Math.Ceiling(TotalRows / (double)PaginationStrip.RowsPerPage));
                    if (TotalPages != 0 && TotalPages < PaginationStrip.CurrentIndex) PaginationStrip.CurrentIndex = 1;
                }
                if (query is IAsyncEnumerable<ListModel>)
                {
                    Items = await (UsePagination ? query.Skip((PaginationStrip.CurrentIndex - 1) * PaginationStrip.RowsPerPage).Take(PaginationStrip.RowsPerPage).ToListAsync() : query.ToListAsync());
                }
                else
                {
                    Items = UsePagination ? query.Skip((PaginationStrip.CurrentIndex - 1) * PaginationStrip.RowsPerPage).Take(PaginationStrip.RowsPerPage).ToList() : query.ToList();
                }
                SNo = PaginationStrip.CurrentIndex;
                await ItemsLoaded();
            }
            catch (Exception ex)
            {
                Error = ex.Message + ex.InnerException?.Message;
            }
            await UnBlockTableUI();
        }

        protected abstract IQueryable<ListModel> GetPageAsync();


        /// <summary>
        /// This method shall transform SelectedItem object to Data Models and add/update them to Database
        /// </summary>
        /// <returns></returns>
        protected abstract Task PostAsync();

        protected void HideModal()
        {
            ModalCss = null;
            BackDrop = "";
            ButtonClasses = null;
            SelectedItem = null;
            ValidationError = null;
        }

        protected void ShowModal()
        {
            ButtonClasses = "btn btn-primary pull-right";
            ModalCss = "offcanvas-on";
            BackDrop = "offcanvas-overlay";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
