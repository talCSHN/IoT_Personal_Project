using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Schema;
using WpfMrpSimulatorApp.Helpers;
using WpfMrpSimulatorApp.Models;

namespace WpfMrpSimulatorApp.ViewModels
{   
    public partial class ScheduleViewModel : ObservableObject
    {
        // readonly 생성자에서 할당하고나면 그 이후에 값변경 불가
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IoTDbContext dbContext;

        #region View와 연동할 멤버변수들

        private DateTime? _regDt;
        private DateTime? _modDt;

        private ObservableCollection<ScheduleNew> _schedules;
        private ScheduleNew _selectedSchedule;

        private ObservableCollection<Setting> _plantCodes;
        private ObservableCollection<Setting> _facilityIds;

        private bool _isUpdate;

        private bool _canSave;
        private bool _canRemove;

        #endregion

        #region View와 연동할 속성


        // 시작시간, 종료시간용 데이터 속성
        public ObservableCollection<TimeOption> TimeOptions { get; }
            = new ObservableCollection<TimeOption>(
                Enumerable.Range(0, 24).Select(h => new TimeOption
                {
                    Time = new TimeOnly(h, 0),
                    Display = $"{h:00}:00"
                })
            );

        // 플랜트코드 콤보박스용 데이터 속성
        public ObservableCollection<Setting> PlantCodes { 
            get => _plantCodes;
            set => SetProperty(ref _plantCodes, value);
        }
        // 설비아이디 콤보박스용 데이터 속성
        public ObservableCollection<Setting> FacilityIds
        {
            get => _facilityIds;
            set => SetProperty(ref _facilityIds, value);
        }

        public bool CanSave
        {
            get => _canSave;
            set => SetProperty(ref _canSave, value);
        }

        public bool CanRemove
        {
            get => _canRemove;
            set => SetProperty(ref _canRemove, value);
        }

        public bool IsUpdate
        {
            get { return _isUpdate; }
            set { SetProperty(ref _isUpdate, value); }
        }

        // View와 연동될 데이터/컬렉션
        public ObservableCollection<ScheduleNew> Schedules
        {
            get => _schedules;
            set => SetProperty(ref _schedules, value);
        }

        public ScheduleNew SelectedSchedule
        {
            get => _selectedSchedule;
            set { 
                SetProperty(ref _selectedSchedule, value);
                // 최초에 BasicCode에 값이 있는 상태만 수정상태
                if (_selectedSchedule != null)  // 삭제 후에는 _selectedSetting자체가 null이 됨
                {
                    if (_selectedSchedule.SchIdx > 0)
                        CanSave = CanRemove = true; // 기존데이터가 있으면 수정, 삭제 가능
                }
            }
        }

        public DateTime? RegDt {
            get => _regDt;
            set => SetProperty(ref _regDt, value);
        }

        public DateTime? ModDt {
            get => _modDt;
            set => SetProperty(ref _modDt, value);
        }

        #endregion

        public ScheduleViewModel(IDialogCoordinator coordinator)
        {
            this.dialogCoordinator = coordinator;  // 파라미터값으로 초기화
            this.dbContext = new IoTDbContext();

            InitComboboxes(); // DB에서 데이터로드 후 콤보박스에 들어가는 데이터할당 초기화 
            LoadGridFromDb(); // DB에서 데이터로드해서 그리드에 출력
            IsUpdate = true;

            // 최초에는 저장버튼, 삭제버튼이 비활성화 
            CanSave = CanRemove = false;            
        }

        private void InitComboboxes()
        {
            using (var db = new IoTDbContext())
            {
                var plants = db.Settings.Where(s => s.BasicCode.StartsWith("PLT")).ToList();
                PlantCodes = new ObservableCollection<Setting>(plants);

                var facilitys = db.Settings.Where(s => s.BasicCode.StartsWith("FAC")).ToList();
                FacilityIds = new ObservableCollection<Setting>(facilitys);
            }
        }

        private async Task LoadGridFromDb()
        {
            try
            {
                using (var db = new IoTDbContext())
                {
                    var results = db.Schedules
                                    .Join(db.Settings,
                                        sch => sch.PlantCode,
                                        setting => setting.BasicCode,
                                        (sch, setting1) => new { sch, setting1 })
                                    .Join(db.Settings,
                                          temp => temp.sch.SchFacilityId,
                                          setting2 => setting2.BasicCode,
                                          (temp, setting2) => new ScheduleNew
                                          {
                                            SchIdx = temp.sch.SchIdx,
                                            PlantCode = temp.sch.PlantCode,
                                            PlantName = temp.setting1.CodeName,  // 첫번째 조인에서 만든 값
                                            SchDate = temp.sch.SchDate,
                                            LoadTime = temp.sch.LoadTime,
                                            SchStartTime = temp.sch.SchStartTime,
                                            SchEndTime = temp.sch.SchEndTime,
                                            SchFacilityId = temp.sch.SchFacilityId,
                                            SchFacilityName = setting2.CodeName,  // 두번째 조인에서 만든 값
                                            SchAmount = temp.sch.SchAmount,
                                            RegDt = temp.sch.RegDt,
                                            ModDt = temp.sch.ModDt,
                                          }
                                    ).ToList();

                    ObservableCollection<ScheduleNew> schedules = new ObservableCollection<ScheduleNew>(results);
                    Schedules = schedules;
                }                
            }
            catch (Exception ex)
            {
                await this.dialogCoordinator.ShowMessageAsync(this, "오류", ex.Message);
            }
        }

        private void InitVariable()
        {
            SelectedSchedule = new ScheduleNew();
            // SelectedSchedule.SchDate = DateOnly.FromDateTime(DateTime.Now); // 신규버튼 눌렀을때 0001-01-01방지
            // IsUpdate가 False면 신규, True면 수정
            IsUpdate = false;
        }

        #region View 버튼클릭 메서드

        [RelayCommand]
        public void NewData()
        {
            InitVariable();
            IsUpdate = false;  // DoubleCheck. 확실하게 동작을 하면 지워도 되는 로직
            CanSave = true; // 저장버튼 활성화
            CanRemove = false; // 삭제버튼 비활성화
        }        

        [RelayCommand]
        public async Task SaveData()
        {
            // INSERT, UPDATE 기능을 모두 수행
            try
            {
                // SelectedSchedule 형 ScheduleNew --> Schedule객체로 바꿔서 저장, 수정해야함
                var schedule = new Schedule
                {
                    SchIdx = SelectedSchedule.SchIdx,
                    PlantCode = SelectedSchedule.PlantCode,
                    SchDate = SelectedSchedule.SchDate,
                    LoadTime = SelectedSchedule.LoadTime,
                    SchStartTime = SelectedSchedule.SchStartTime,
                    SchEndTime = SelectedSchedule.SchEndTime,
                    SchFacilityId = SelectedSchedule.SchFacilityId,
                    SchAmount = SelectedSchedule.SchAmount                    
                };

                using (var db = new IoTDbContext())
                {
                    if (schedule.SchIdx == 0) // 신규
                    {
                        schedule.RegDt = DateTime.Now;  // 등록일 현재일자
                        db.Schedules.Add(schedule); // ASP.NET Core에서 한 작업과 동일
                    }
                    else  // 수정
                    {
                        var origin = db.Schedules.Find(schedule.SchIdx); // ASP.NET Core와 동일
                        if (origin != null)
                        {
                            origin.PlantCode = schedule.PlantCode;
                            origin.SchDate = schedule.SchDate;
                            origin.LoadTime = schedule.LoadTime;
                            origin.SchStartTime = schedule.SchStartTime;
                            origin.SchEndTime = schedule.SchEndTime;
                            origin.SchFacilityId = schedule.SchFacilityId;
                            origin.SchAmount = schedule.SchAmount;
                            origin.ModDt = DateTime.Now;
                        }
                    }
                    db.SaveChanges(); // COMMIT
                    await this.dialogCoordinator.ShowMessageAsync(this, "공정계획 저장", "데이터가 저장되었습니다.");
                }
            }
            catch (Exception ex)
            {
                await this.dialogCoordinator.ShowMessageAsync(this, "오류", ex.Message);
            }

            LoadGridFromDb(); // 재조회
            IsUpdate = true;  // 다시 입력안되도록 막기
        }

        [RelayCommand]
        public async Task RemoveData()
        {
            var result = await this.dialogCoordinator.ShowMessageAsync(this, "삭제", "삭제하시겠습니까?", MessageDialogStyle.AffirmativeAndNegative);
            if (result == MessageDialogResult.Negative) return; // Cancel을 누르면 메서드를 탈출

            try
            {
                using (var db = new IoTDbContext())
                {
                    var entity = db.Schedules.Find(SelectedSchedule.SchIdx);
                    if (entity != null)
                    {
                        db.Schedules.Remove(entity);
                        db.SaveChanges(); // COMMIT
                    }
                }

                await this.dialogCoordinator.ShowMessageAsync(this, "공정계획 삭제", "데이터가 삭제되었습니다.");
            }
            catch (Exception ex)
            {
                await this.dialogCoordinator.ShowMessageAsync(this, "오류", ex.Message);
            }

            LoadGridFromDb();  // DB를 다시 불러서 그리드를 재조회한다.
            IsUpdate = true;  // 다시 입력안되도록 막기
        }

        #endregion
    }
}
