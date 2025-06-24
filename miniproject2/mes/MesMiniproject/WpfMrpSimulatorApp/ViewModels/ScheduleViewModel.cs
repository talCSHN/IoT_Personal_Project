using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MahApps.Metro.Controls.Dialogs;
using MySql.Data.MySqlClient;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
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
        private bool _isUpdate;

        private bool _canSave;
        private bool _canRemove;

        #endregion

        #region View와 연동할 속성

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
            set
            {
                SetProperty(ref _selectedSchedule, value);
                // 최초에 BasicCode에 값이 있는 상태만 수정상태
                if (_selectedSchedule != null)  // 삭제 후에는 _selectedSetting자체가 null이 됨
                {
                    //    if (_selectedSchedule.SchIdx != null)  // NullReferenceException 발생 가능
                    //    {
                    //        CanSave = true;
                    //        CanRemove = true;
                    //    }
                }
            }
        }

        public DateTime? RegDt
        {
            get => _regDt;
            set => SetProperty(ref _regDt, value);
        }

        public DateTime? ModDt
        {
            get => _modDt;
            set => SetProperty(ref _modDt, value);
        }

        #endregion

        public ScheduleViewModel(IDialogCoordinator coordinator)
        {
            this.dialogCoordinator = coordinator;  // 파라미터값으로 초기화
            this.dbContext = new IoTDbContext();

            LoadGridFromDb(); // DB에서 데이터로드해서 그리드에 출력
            IsUpdate = true;

            // 최초에는 저장버튼, 삭제버튼이 비활성화 
            CanSave = CanRemove = false;
        }

        private async Task LoadGridFromDb()
        {
            try
            {
                Console.WriteLine("LoadGridFromDb 실행");
                using (var db = new IoTDbContext())
                {
                    Console.WriteLine("using문 실행");
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
                                            PlantName = temp.setting1.CodeName, // 첫 번째 조인에서 가져옴
                                            SchDate = temp.sch.SchDate,
                                            LoadTime = temp.sch.LoadTime,
                                            SchStartTime = temp.sch.SchStartTime,
                                            SchEndTime = temp.sch.SchEndTime,
                                            SchFacilityId = temp.sch.SchFacilityId,
                                            SchFacilityName = setting2.CodeName, // 두 번째 조인에서 가져옴
                                            SchAmount = temp.sch.SchAmount,
                                            RegDt = temp.sch.RegDt,
                                            ModDt = temp.sch.ModDt,
                                        }
                                    ).ToList();
                    results.ForEach(x => Console.WriteLine(x.ToString()));
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
            // IsUpdate가 False면 신규, True면 수정
            IsUpdate = false;

            CanSave = true;
            CanRemove = false;  // 이게 없으면 수정후 신규를 눌러도 활성화 되어 있음.
        }

        #region View 버튼클릭 메서드

        [RelayCommand]
        public void NewData()
        {
            InitVariable();
            IsUpdate = false;  // DoubleCheck. 확실하게 동작을 하면 지워도 되는 로직
            CanSave = true; // 저장버튼 활성화
        }

        [RelayCommand]
        public async Task SaveData()
        {
            // INSERT, UPDATE 기능을 모두 수행
            try
            {
                string query = string.Empty;

                using (MySqlConnection conn = new MySqlConnection(Common.CONNSTR))
                {
                    conn.Open();

                    if (IsUpdate)
                    {
                        query = @"UPDATE settings SET codeName = @codeName, codeDesc = @codeDesc, modDt = now() 
                                   WHERE basicCode = @basicCode";  // UPDATE 쿼리
                    }
                    else
                    {
                        query = @"INSERT INTO settings (basicCode, codeName, codeDesc, regDt)
                                   VALUES (@basicCode, @codeName, @codeDesc, now());"; // INSERT 쿼리
                    }

                    MySqlCommand cmd = new MySqlCommand(query, conn);

                    var resultCnt = cmd.ExecuteNonQuery();
                    if (resultCnt > 0)
                    {
                        await this.dialogCoordinator.ShowMessageAsync(this, "기본설정 저장", "데이터가 저장되었습니다.");
                    }
                    else
                    {
                        await this.dialogCoordinator.ShowMessageAsync(this, "기본설정 저장", "데이터가 저장에 실패했습니다.");
                    }
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
                string query = "DELETE FROM settings WHERE basicCode = @basicCode";

                using (MySqlConnection conn = new MySqlConnection(Common.CONNSTR))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand(query, conn);

                    int resultCnt = cmd.ExecuteNonQuery(); // 삭제된 쿼리행수 리턴 1, 안지워졌으면 0

                    if (resultCnt == 1)
                    {
                        await this.dialogCoordinator.ShowMessageAsync(this, "기본설정 삭제", "데이터가 삭제되었습니다.");
                    }
                    else
                    {
                        await this.dialogCoordinator.ShowMessageAsync(this, "기본설정 삭제", "데이터가 삭제 문제발생!!");
                    }
                }
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