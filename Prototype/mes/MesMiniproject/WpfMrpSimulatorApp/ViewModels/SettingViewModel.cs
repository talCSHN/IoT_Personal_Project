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
    public partial class SettingViewModel : ObservableObject
    {
        // readonly 생성자에서 할당하고나면 그 이후에 값변경 불가
        private readonly IDialogCoordinator dialogCoordinator;

        #region View와 연동할 멤버변수들

        private string _basicCode;
        private string _codeName;
        private string? _codeDesc;
        private DateTime? _regDt;
        private DateTime? _modDt;

        private ObservableCollection<Setting> _settings;
        private Setting _selectedSetting;
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
        public ObservableCollection<Setting> Settings
        {
            get => _settings;
            set => SetProperty(ref _settings, value);
        }

        public Setting SelectedSetting
        {
            get => _selectedSetting;
            set { 
                SetProperty(ref _selectedSetting, value);
                // 최초에 BasicCode에 값이 있는 상태만 수정상태
                if (_selectedSetting != null)  // 삭제 후에는 _selectedSetting자체가 null이 됨
                {
                    if (!string.IsNullOrEmpty(_selectedSetting.BasicCode))  // NullReferenceException 발생 가능
                    {
                        CanSave = true;
                        CanRemove = true;
                    }
                }
            }
        }

        /// <summary>
        /// 기본코드
        /// </summary>
        public string BasicCode {
            get => _basicCode;
            set => SetProperty(ref _basicCode, value);
        }

        /// <summary>
        /// 코드명
        /// </summary>
        public string CodeName {
            get => _codeName; 
            set => SetProperty(ref _codeName, value);
        }

        /// <summary>
        /// 코드설명
        /// </summary>
        public string? CodeDesc {
            get => _codeDesc;
            set => SetProperty(ref _codeDesc, value);
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
        public SettingViewModel(IDialogCoordinator coordinator)
        {
            this.dialogCoordinator = coordinator;  // 파라미터값으로 초기화

            LoadGridFromDb(); // DB에서 데이터로드해서 그리드에 출력
            IsUpdate = true;

            // 최초에는 저장버튼, 삭제버튼이 비활성화 
            CanSave = CanRemove = false;            
        }

        private async Task LoadGridFromDb()
        {
            try
            {
                string query = @"SELECT basicCode
                                      , codeName
                                      , codeDesc
                                      , regDt
                                      , modDt
                                   FROM settings";
                ObservableCollection<Setting> settings = new ObservableCollection<Setting>();

                // DB연동 방식 1
                using (MySqlConnection conn = new MySqlConnection(Common.CONNSTR))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    MySqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        var basicCode = reader.GetString("basicCode");
                        var codeName = reader.GetString("codeName");
                        var codeDesc = reader.GetString("codeDesc");
                        var regDt = reader.GetDateTime("regDt");
                        // modDt는 최초에 입력후 항상 null. NULL타입 체크 필수
                        var modDt = reader.IsDBNull(reader.GetOrdinal("modDt")) ? (DateTime?)null : reader.GetDateTime("modDt");

                        settings.Add(new Setting
                        {
                            BasicCode = basicCode,
                            CodeName = codeName,
                            CodeDesc = codeDesc,
                            RegDt = regDt,
                            ModDt = modDt,
                        });
                    }
                }

                Settings = settings;
            }
            catch (Exception ex)
            {
                await this.dialogCoordinator.ShowMessageAsync(this, "오류", ex.Message);
            }
        }

        private void InitVariable()
        {
            SelectedSetting = new Setting();
            // IsUpdate가 False면 신규, True면 수정
            IsUpdate = false;

            CanSave = true;
            CanRemove = false;  // 이게 없으면 수정후 신규를 눌러도 활성화 되어 있음. 250623 12:31 hugo 수정
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
                    cmd.Parameters.AddWithValue("@basicCode", SelectedSetting.BasicCode);
                    cmd.Parameters.AddWithValue("@codeName", SelectedSetting.CodeName);
                    cmd.Parameters.AddWithValue("@codeDesc", SelectedSetting.CodeDesc);

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

                    cmd.Parameters.AddWithValue("@basicCode", SelectedSetting.BasicCode);

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
