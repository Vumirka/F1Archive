using F1Archive.Database;
using F1Archive.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace F1Archive.Editing
{
    // Дані однієї гонки: результати, кваліфікація і піт-стопи.
    // Гонка вибирається один раз, а перемикач показує потрібну таблицю і форму.
    public partial class RaceEdit : EditPage
    {
        // Що редагуємо зараз: "results", "qualifying" або "pits"
        private string _mode = "results";

        public RaceEdit()
        {
            InitializeComponent();
            SetControls(StatusText, DeleteButton);
            DriverBox.ItemsSource = EditHelper.Drivers();
            TeamBox.ItemsSource = EditHelper.Teams();
            SeasonFilter.ItemsSource = EditHelper.Seasons();
            SeasonFilter.SelectedIndex = 0;
            ResultsMode.IsChecked = true;
        }

        // Змінили сезон - показуємо його Гран-прі (вибираємо останнє)
        private void Season_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (EditHelper.Selected(SeasonFilter) is int seasonId)
            {
                RaceFilter.ItemsSource = EditHelper.GrandPrix(seasonId);
                RaceFilter.SelectedIndex = RaceFilter.Items.Count - 1;
            }
        }

        private void Race_Changed(object sender, SelectionChangedEventArgs e)
        {
            LoadList();
            NewRecord();
        }

        private void Mode_Checked(object sender, RoutedEventArgs e)
        {
            var button = e.OriginalSource as RadioButton;

            if (button == null || button.Tag == null)
            {
                return;
            }

            string mode = (string)button.Tag;
            _mode = mode;

            ResultsList.Visibility = mode == "results" ? Visibility.Visible : Visibility.Collapsed;
            QualifyingList.Visibility = mode == "qualifying" ? Visibility.Visible : Visibility.Collapsed;
            PitStopsList.Visibility = mode == "pits" ? Visibility.Visible : Visibility.Collapsed;

            ResultsForm.Visibility = ResultsList.Visibility;
            QualifyingForm.Visibility = QualifyingList.Visibility;
            PitStopsForm.Visibility = PitStopsList.Visibility;
            TeamPanel.Visibility = mode == "pits" ? Visibility.Collapsed : Visibility.Visible;

            NewRecord();
        }

        protected override void LoadList()
        {
            using var db = new F1ArchiveContext();
            int? raceId = EditHelper.Selected(RaceFilter);

            ResultsList.ItemsSource = db.Results
                .Include(r => r.Driver)
                .Include(r => r.Team)
                .Where(r => r.GrandPrixId == raceId)
                .OrderBy(r => r.FinishPosition == null)
                .ThenBy(r => r.FinishPosition)
                .ToList();

            QualifyingList.ItemsSource = db.QualifyingResults
                .Include(q => q.Driver)
                .Include(q => q.Team)
                .Where(q => q.GrandPrixId == raceId)
                .OrderBy(q => q.Position)
                .ToList();

            PitStopsList.ItemsSource = db.PitStops
                .Include(p => p.Driver)
                .Where(p => p.GrandPrixId == raceId)
                .OrderBy(p => p.Lap)
                .ToList();
        }

        protected override void ClearForm()
        {
            if (_mode == "qualifying")
            {
                FormTitle.Text = "Новий рядок кваліфікації";
            }
            else if (_mode == "pits")
            {
                FormTitle.Text = "Новий піт-стоп";
            }
            else
            {
                FormTitle.Text = "Новий результат";
            }

            DriverBox.SelectedValue = null;
            TeamBox.SelectedValue = null;

            FinishBox.Text = "";
            StartBox.Text = "";
            PointsBox.Text = "";
            StatusBox.Text = ResultStatus.Finished;
            NumberBox.Text = "";
            LapsBox.Text = "";
            TimeBox.Text = "";
            SharedBox.IsChecked = false;

            PositionBox.Text = (QualifyingList.Items.Count + 1).ToString();
            Q1Box.Text = "";
            Q2Box.Text = "";
            Q3Box.Text = "";

            StopBox.Text = "1";
            PitLapBox.Text = "";
            DurationBox.Text = "";
        }

        protected override void ShowRecord(object item)
        {
            if (item is Result result)
            {
                EditId = result.Id;
                FormTitle.Text = result.Driver.FirstName + " " + result.Driver.LastName;
                DriverBox.SelectedValue = result.DriverId;
                TeamBox.SelectedValue = result.TeamId;
                FinishBox.Text = result.FinishPosition.ToString();
                StartBox.Text = result.StartPosition.ToString();
                PointsBox.Text = result.Points.ToString();
                StatusBox.Text = result.Status;
                NumberBox.Text = result.CarNumber.ToString();
                LapsBox.Text = result.LapsCompleted.ToString();
                TimeBox.Text = result.TimeOrRetired;
                SharedBox.IsChecked = result.IsSharedDrive;
            }
            else if (item is QualifyingResult row)
            {
                EditId = row.Id;
                FormTitle.Text = row.Driver.FirstName + " " + row.Driver.LastName;
                DriverBox.SelectedValue = row.DriverId;
                TeamBox.SelectedValue = row.TeamId;
                PositionBox.Text = row.Position.ToString();
                Q1Box.Text = row.Q1 ?? "";
                Q2Box.Text = row.Q2 ?? "";
                Q3Box.Text = row.Q3 ?? "";
            }
            else if (item is PitStop stop)
            {
                EditId = stop.Id;
                FormTitle.Text = stop.Driver.FirstName + " " + stop.Driver.LastName;
                DriverBox.SelectedValue = stop.DriverId;
                StopBox.Text = stop.StopNumber.ToString();
                PitLapBox.Text = stop.Lap.ToString();
                DurationBox.Text = stop.Duration;
            }
        }

        protected override int? SaveRecord()
        {
            int? raceId = EditHelper.Selected(RaceFilter);
            int? driverId = EditHelper.Selected(DriverBox);
            int? teamId = EditHelper.Selected(TeamBox);

            if (raceId == null || driverId == null || (_mode != "pits" && teamId == null))
            {
                ShowStatus("Оберіть Гран-прі, гонщика і команду.");

                return null;
            }

            using var db = new F1ArchiveContext();
            int? id;

            if (_mode == "results")
            {
                Result result = EditId == null ? new Result() : db.Results.Find(EditId)!;

                result.GrandPrixId = raceId.Value;
                result.DriverId = driverId.Value;
                result.TeamId = teamId!.Value;
                result.FinishPosition = EditHelper.Int(FinishBox);
                result.StartPosition = EditHelper.Int(StartBox);
                result.Points = EditHelper.Number(PointsBox) ?? 0;
                result.Status = EditHelper.Text(StatusBox);
                result.CarNumber = EditHelper.Int(NumberBox);
                result.LapsCompleted = EditHelper.Int(LapsBox);
                result.TimeOrRetired = EditHelper.Text(TimeBox);
                result.IsSharedDrive = SharedBox.IsChecked == true;

                if (EditId == null)
                {
                    db.Results.Add(result);
                }

                if (!EditHelper.Save(db))
                {
                    ShowStatus("Не вдалося зберегти результат.");

                    return null;
                }

                id = result.Id;
            }
            else if (_mode == "qualifying")
            {
                QualifyingResult row = EditId == null ? new QualifyingResult() : db.QualifyingResults.Find(EditId)!;

                row.GrandPrixId = raceId.Value;
                row.DriverId = driverId.Value;
                row.TeamId = teamId!.Value;
                row.Position = EditHelper.Int(PositionBox);
                row.Q1 = EditHelper.Text(Q1Box) == "" ? null : EditHelper.Text(Q1Box);
                row.Q2 = EditHelper.Text(Q2Box) == "" ? null : EditHelper.Text(Q2Box);
                row.Q3 = EditHelper.Text(Q3Box) == "" ? null : EditHelper.Text(Q3Box);

                if (EditId == null)
                {
                    db.QualifyingResults.Add(row);
                }

                if (!EditHelper.Save(db))
                {
                    ShowStatus("Цей гонщик уже є в кваліфікації цього Гран-прі.");

                    return null;
                }

                id = row.Id;
            }
            else
            {
                int? stopNumber = EditHelper.Int(StopBox);
                int? lap = EditHelper.Int(PitLapBox);
                int? ms = ToMilliseconds(EditHelper.Text(DurationBox));

                if (stopNumber == null || lap == null || ms == null)
                {
                    ShowStatus("Заповніть номер зупинки, коло і тривалість (23.456).");

                    return null;
                }

                PitStop stop = EditId == null ? new PitStop() : db.PitStops.Find(EditId)!;

                stop.GrandPrixId = raceId.Value;
                stop.DriverId = driverId.Value;
                stop.StopNumber = stopNumber.Value;
                stop.Lap = lap.Value;
                stop.Duration = EditHelper.Text(DurationBox);
                stop.DurationMs = ms;

                if (EditId == null)
                {
                    db.PitStops.Add(stop);
                }

                if (!EditHelper.Save(db))
                {
                    ShowStatus("Така зупинка цього гонщика вже є.");

                    return null;
                }

                id = stop.Id;
            }

            return id;
        }

        protected override string? DeleteRecord()
        {
            using var db = new F1ArchiveContext();

            if (_mode == "results")
            {
                db.Results.Remove(db.Results.Find(EditId)!);
                EditHelper.Save(db);

                return null;
            }
            else if (_mode == "qualifying")
            {
                db.QualifyingResults.Remove(db.QualifyingResults.Find(EditId)!);
            }
            else
            {
                db.PitStops.Remove(db.PitStops.Find(EditId)!);
            }

            EditHelper.Save(db);

            return null;
        }

        // Тривалість "23.456" або "1:02.345" у мілісекундах
        private static int? ToMilliseconds(string text)
        {
            string[] parts = text.Split(':');
            double minutes = 0;

            if (parts.Length == 2 && !double.TryParse(parts[0], out minutes))
            {
                return null;
            }

            // Секунди - остання частина ("02.345" з "1:02.345")
            string secondsText = parts[parts.Length - 1];
            double seconds;

            if (!double.TryParse(secondsText, NumberStyles.Float, CultureInfo.InvariantCulture, out seconds))
            {
                return null;
            }

            return (int)((minutes * 60 + seconds) * 1000);
        }
    }
}
