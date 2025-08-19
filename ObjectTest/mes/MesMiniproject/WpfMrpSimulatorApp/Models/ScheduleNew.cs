using System;
using System.Collections.Generic;

namespace WpfMrpSimulatorApp.Models;

public partial class ScheduleNew
{
    /// <summary>
    /// 공정계획 순번(자동증가)
    /// </summary>
    public int SchIdx { get; set; }

    /// <summary>
    /// 공장코드
    /// </summary>
    public string PlantCode { get; set; } = null!;

    // 데이터그리드에 표현하려면 새로운 속성이 필요!!
    public string PlantName { get; set; }

    /// <summary>
    /// 공정계획일
    /// </summary>
    public DateOnly SchDate { get; set; }

    /// <summary>
    /// 로드타임(초)
    /// </summary>
    public int LoadTime { get; set; }

    /// <summary>
    /// 계획 시작시간
    /// </summary>
    public TimeOnly? SchStartTime { get; set; }

    /// <summary>
    /// 계획 종료시간
    /// </summary>
    public TimeOnly? SchEndTime { get; set; }

    /// <summary>
    /// 생산설비 ID
    /// </summary>
    public string? SchFacilityId { get; set; }

    public string? SchFacilityName { get; set; }

    /// <summary>
    /// 계획목표수량
    /// </summary>
    public int SchAmount { get; set; }

    /// <summary>
    /// 작성일
    /// </summary>
    public DateTime? RegDt { get; set; }

    /// <summary>
    /// 수정일
    /// </summary>
    public DateTime? ModDt { get; set; }
}
