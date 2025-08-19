using System;
using System.Collections.Generic;

namespace WpfMqttSubApp.Models;

public partial class Process
{
    /// <summary>
    /// 공정처리 순번(자동증가)
    /// </summary>
    public int PrcIdx { get; set; }

    public int SchIdx { get; set; }

    /// <summary>
    /// 공정처리 ID(UK) : yyyyMMdd-NewGuid(36)
    /// </summary>
    public string PrcCd { get; set; } = null!;

    /// <summary>
    /// 실제 공정처리일
    /// </summary>
    public DateOnly PrcDate { get; set; }

    /// <summary>
    /// 실제 로드타임
    /// </summary>
    public int PrcLoadTime { get; set; }

    /// <summary>
    /// 실제 시작시간
    /// </summary>
    public TimeOnly? PrcStartTime { get; set; }

    /// <summary>
    /// 실제 종료시간
    /// </summary>
    public TimeOnly? PrcEndTime { get; set; }

    /// <summary>
    /// 실제 공정장비아이디
    /// </summary>
    public string? PrcFacilityId { get; set; }

    /// <summary>
    /// 공정처리 여부(1성공, 0실패)
    /// </summary>
    public sbyte? PrcResult { get; set; }

    /// <summary>
    /// 등록일
    /// </summary>
    public DateTime? RegDt { get; set; }

    /// <summary>
    /// 수정일
    /// </summary>
    public DateTime? ModDt { get; set; }

    public virtual Schedule SchIdxNavigation { get; set; } = null!;
}
