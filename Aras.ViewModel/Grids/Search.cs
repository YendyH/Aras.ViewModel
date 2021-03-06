﻿/*  
  Copyright 2017 Processwall Limited

  Licensed under the Apache License, Version 2.0 (the "License");
  you may not use this file except in compliance with the License.
  You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

  Unless required by applicable law or agreed to in writing, software
  distributed under the License is distributed on an "AS IS" BASIS,
  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  See the License for the specific language governing permissions and
  limitations under the License.
 
  Company: Processwall Limited
  Address: The Winnowing House, Mill Lane, Askham Richard, York, YO23 3NW, United Kingdom
  Tel:     +44 113 815 3440
  Web:     http://www.processwall.com
  Email:   support@processwall.com
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aras.ViewModel.Grids
{
    public class Search : Containers.BorderContainer, IToolbarProvider
    {
        public class ItemsSelectedEventArgs : EventArgs
        {
            public IEnumerable<Model.Item> Items { get; private set; }

            public ItemsSelectedEventArgs(IEnumerable<Model.Item> Items)
                : base()
            {
                this.Items = Items;
            }
        }

        public delegate void ItemsSelectedEventHandler(object sender, ItemsSelectedEventArgs e);

        public event ItemsSelectedEventHandler ItemsSelected;

        private void OnItemsSelected()
        {
            if (this.ItemsSelected != null)
            {
                ItemsSelected(this, new ItemsSelectedEventArgs(this.Selected));
            }
        }

        public IEnumerable<Model.Item> Displayed
        {
            get
            {
                if (this.Query != null)
                {
                    return this.Query.Store;
                }
                else
                {
                    return new List<Model.Item>();
                }
            }
        }

        public Model.ObservableList<Model.Item> Selected { get; private set; }

        public void Select(Model.Item Item)
        {
            if (Item != null)
            {
                List<Model.Item> displayed = this.Displayed.ToList();
                int index = displayed.IndexOf(Item);

                if (index >= 0)
                {
                    this.Grid.SelectedRows.Clear();
                    this.Grid.SelectedRows.Add(this.Grid.Rows[index]);

                    // Update Selected
                    this.Selected.NotifyListChanged = false;

                    this.Selected.Clear();
                    this.Selected.Add(Item);

                    this.Selected.NotifyListChanged = true;

                    // Trigger Event
                    this.OnItemsSelected();
                }
            }
        }

        public Model.Item Add(Model.Item Item)
        {
            Model.Item ret = null;

            if (Item != null)
            {
                if (this.Query != null)
                {
                    ret = this.Query.Store.Add(Item);
                    this.LoadRows();
                }
            }

            return ret;
        }

        private Model.Query _query;
        private Model.Query Query
        {
            get
            {
                if (this._query == null)
                {
                    if (this.Binding != null)
                    {
                        this._query = ((Model.Store)this.Binding).Query;
                        this._query.PageSize = (System.Int32)this.PageSize.Value;
                        this._query.Paging = true;
                    }
                }

                return this._query;
            }
        }

        private Dialogs.Filters _dialog;
        public Dialogs.Filters Dialog
        {
            get
            {
                if (this._dialog == null)
                {
                    this._dialog = new Dialogs.Filters(this, this.Query.Store.ItemType.SearchPropertyTypes);
                    this._dialog.PropertyChanged += _dialog_PropertyChanged;
                }

                return this._dialog;
            }
        }

        private void _dialog_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Open")
            {
                if (!this.Dialog.Open)
                {
                    this.RefreshControl();
                }
            }
        }

        private Containers.Toolbar _toolbar;
        public virtual Containers.Toolbar Toolbar
        {
            get
            {
                if (this._toolbar == null)
                {
                    // Create Toolbar
                    this._toolbar = new Containers.Toolbar(this.Session);

                    // Stop Notification
                    this._toolbar.Children.NotifyListChanged = false;

                    // Add Search Button
                    Button searchbutton = new Button(this.Session);
                    searchbutton.Icon = "Search";
                    searchbutton.Tooltip = "Search";
                    this._toolbar.Children.Add(searchbutton);
                    searchbutton.Command = this.Refresh;

                    // Add Filter Button
                    Button filterbutton = new Button(this.Session);
                    filterbutton.Icon = "AddFilter";
                    filterbutton.Tooltip = "Add Filters";
                    this._toolbar.Children.Add(filterbutton);
                    filterbutton.Command = this.Filters;

                    // Add Clear Button
                    Button clearbutton = new Button(this.Session);
                    clearbutton.Icon = "ClearFilter";
                    clearbutton.Tooltip = "Clear Filters";
                    this._toolbar.Children.Add(clearbutton);
                    clearbutton.Command = this.Clear;

                    // Add Page Size
                    this._toolbar.Children.Add(this.PageSize);

                    // Add Next Page Button
                    Button nextbutton = new Button(this.Session);
                    nextbutton.Icon = "NextPage";
                    nextbutton.Tooltip = "Next Page";
                    this._toolbar.Children.Add(nextbutton);
                    nextbutton.Command = this.NextPage;

                    // Add Previous Page Button
                    Button previousbutton = new Button(this.Session);
                    previousbutton.Icon = "PreviousPage";
                    previousbutton.Tooltip = "Previous Page";
                    this._toolbar.Children.Add(previousbutton);
                    previousbutton.Command = this.PreviousPage;

                    // Start Notification
                    this._toolbar.Children.NotifyListChanged = true;
                }

                return this._toolbar;
            }
        }

        protected ViewModel.Grid Grid { get; private set; }

        [ViewModel.Attributes.Command("Refresh")]
        public RefreshCommand Refresh { get; private set; }

        [ViewModel.Attributes.Command("Filters")]
        public FiltersCommand Filters { get; private set; }

        [ViewModel.Attributes.Command("Clear")]
        public ClearCommand Clear { get; private set; }

        [ViewModel.Attributes.Command("NextPage")]
        public NextPageCommand NextPage { get; private set; }

        [ViewModel.Attributes.Command("PreviousPage")]
        public PreviousPageCommand PreviousPage { get; private set; }

        public Properties.Integers.Spinner PageSize { get; private set; }

        public Properties.Integer Page { get; protected set; }

        public Properties.Integer NoPages { get; protected set; }

        private void LoadRows()
        {
            if (this.Query != null)
            {
                this.Grid.NoRows = this.Query.Store.Count();

                for (int i = 0; i < this.Grid.NoRows; i++)
                {
                    Model.Item item = this.Query.Store[i];
                    int j = 0;

                    foreach (Model.PropertyType proptype in this.Query.Store.ItemType.SearchPropertyTypes)
                    {
                        Model.Property property = item.Property(proptype);
                        this.Grid.Rows[i].Cells[j].Binding = property;
                        j++;
                    }
                }
            }
            else
            {
                // Clear all Rows
                this.Grid.NoRows = 0;
            }
        }

        protected void LoadColumns()
        {
            if (this.Binding != null)
            {
                this.Grid.Columns.Clear();

                foreach (Model.PropertyType proptype in this.Query.Store.ItemType.SearchPropertyTypes)
                {
                    this.Grid.AddColumn(proptype);
                }
            }
            else
            {
                // Clear Columns
                this.Grid.Columns.Clear();
            }
        }

        protected override void AfterBindingChanged()
        {
            base.AfterBindingChanged();

            // Reset Query
            this._query = null;

            // Load Columns
            this.LoadColumns();

            // Load Rows
            this.RefreshControl();
        }

        private void PageSize_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Value")
            {
                // Set Page to 1
                this.Page.Value = 1;

                this.RefreshControl();
            }
        }

        protected void RefreshControl()
        {
            if (this.Query != null)
            {              
                // Set Condition
                this.Query.Condition = this.Dialog.Condition();

                // Set PageSize and required Page
                this.Query.PageSize = (System.Int32)this.PageSize.Value;
                this.Query.Page = (System.Int32)this.Page.Value;

                // Refresh Query
                this.Query.Store.Refresh();

                // Update NoPages
                this.NoPages.Value = this.Query.Store.NoPages;
            }
            else
            {
                this.NoPages.Value = 0;
            }

            // Load Grid
            this.LoadRows();

            // Refresh Buttons
            this.NextPage.Refesh();
            this.PreviousPage.Refesh();
        }

        private void DisplayFilters()
        {
            this.Dialog.Open = true;
        }

        private void ClearFilters()
        {
            // Clear Filters
            this.Dialog.Clear();

            // Run Search
            this.RefreshControl();
        }

        private void Grid_RowsSelected(object sender, Grid.RowsSelectedEventArgs e)
        {
            this.Selected.NotifyListChanged = false;
            this.Selected.Clear();

            List<Model.Item> items = this.Displayed.ToList();

            foreach (Row row in this.Grid.SelectedRows)
            {
                this.Selected.Add(items[row.Index]);
            }

            this.Selected.NotifyListChanged = true;

            // Trigger Event
            this.OnItemsSelected();
        }

        public Search(Manager.Session Session)
            : base(Session)
        {
            // Create Selected
            this.Selected = new Model.ObservableList<Model.Item>();

            // Create Page
            this.Page = new Properties.Integer(this.Session);
            this.Page.Value = 1;

            // Create No Pages
            this.NoPages = new Properties.Integer(this.Session);
            this.NoPages.Value = 0;

            // Create Grid
            this.Grid = new Grid(this.Session);
            this.Grid.AllowSelect = true;
            this.Grid.Width = this.Width;
            this.Children.Add(this.Grid);

            // Watch for Rows Selected in Grid
            this.Grid.RowsSelected += Grid_RowsSelected;

            // Create Commands
            this.Refresh = new RefreshCommand(this);
            this.Filters = new FiltersCommand(this);
            this.Clear = new ClearCommand(this);
            this.NextPage = new NextPageCommand(this);
            this.PreviousPage = new PreviousPageCommand(this);

            // Create Page Size
            this.PageSize = new Properties.Integers.Spinner(this.Session);
            this.PageSize.Tooltip = "Page Size";
            this.PageSize.Width = 40;
            this.PageSize.Enabled = true;
            this.PageSize.MinValue = 5;
            this.PageSize.MaxValue = 100;
            this.PageSize.Value = 25;
            this.PageSize.PropertyChanged += PageSize_PropertyChanged;
        }

        public class RefreshCommand : Aras.ViewModel.Command
        {
            protected override void Run(IEnumerable<Control> Parameters)
            {
                ((Search)this.Control).RefreshControl();
                this.CanExecute = true;
            }

            internal RefreshCommand(Control Control)
                : base(Control)
            {
                this.CanExecute = true;
            }
        }

        public class FiltersCommand : Aras.ViewModel.Command
        {
            protected override void Run(IEnumerable<Control> Parameters)
            {
                ((Search)this.Control).DisplayFilters();
                this.CanExecute = true;
            }

            internal FiltersCommand(Control Control)
                : base(Control)
            {
                this.CanExecute = true;
            }
        }

        public class ClearCommand : Aras.ViewModel.Command
        {
            protected override void Run(IEnumerable<Control> Parameters)
            {
                ((Search)this.Control).ClearFilters();
                this.CanExecute = true;
            }

            internal ClearCommand(Control Control)
                : base(Control)
            {
                this.CanExecute = true;
            }
        }

        public class NextPageCommand : Aras.ViewModel.Command
        {
            public Search Search
            {
                get
                {
                    return ((Search)this.Control);
                }
            }

            protected override void Run(IEnumerable<Control> Parameters)
            {
                if (this.Search.Page.Value < this.Search.NoPages.Value)
                {
                    this.Search.Page.Value = this.Search.Page.Value + 1;
                    this.Search.RefreshControl();
                }
            }

            internal void Refesh()
            {
                if (this.Search.Page.Value < this.Search.NoPages.Value)
                {
                    this.CanExecute = true;
                }
                else
                {
                    this.CanExecute = false;
                }
            }

            internal NextPageCommand(Search Search)
                :base(Search)
            {
                this.Refesh();
            }
        }

        public class PreviousPageCommand : Aras.ViewModel.Command
        {
            public Search Search
            {
                get
                {
                    return ((Search)this.Control);
                }
            }

            protected override void Run(IEnumerable<Control> Parameters)
            {
                if (this.Search.Page.Value > 1)
                {
                    this.Search.Page.Value = this.Search.Page.Value - 1;
                    this.Search.RefreshControl();
                }
            }

            internal void Refesh()
            {
                if (this.Search.Page.Value > 1)
                {
                    this.CanExecute = true;
                }
                else
                {
                    this.CanExecute = false;
                }
            }

            internal PreviousPageCommand(Search Search)
                :base(Search)
            {
                this.Refesh();
            }
        }
    }
}
