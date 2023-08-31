using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Frigate;

public class FrigateEvent
{
    public FrigateEventDetail? before { get; set; }
    public FrigateEventDetail? after { get; set; }
    // new, update, end
    public string type { get; set; } //"end"
}

public class FrigateEventDetail
{
    public string id { get; set; } //1682381716.884306-oj8ub5
    public string camera { get; set; } //laundry_cam
    public float frame_time { get; set; } //1682381731.204078
    public float snapshot_time { get; set; } //1682381731.007501
    public string label { get; set; } //person
    public string? sub_label { get; set; }
    public float top_score { get; set; } //0.83203125
    public bool false_positive { get; set; }

    public float? start_time { get; set; } //1682381716.884306
    public float? end_time { get; set; } //1682381736.402552
    public float score { get; set; } //0.546875
    public IEnumerable<uint> box { get; set; } //[96, 397, 253, 479]
    public uint area { get; set; } //12874
    public float ratio { get; set; } //1.9146341463414633
    public IEnumerable<uint>? region { get; set; } //[14, 160, 334, 480]
    /// <summary>
    /// Whether or not the object is considered stationary.
    /// </summary>
    public bool stationary { get; set; }
    /// <summary>
    /// Number of frames the object has been motionless.
    /// </summary>
    public uint motionless_count { get; set; } //0
    /// <summary>
    /// Number of times the object has moved from a stationary position.
    /// </summary>
    public uint position_changes { get; set; } //1
    public IEnumerable<string>? current_zones { get; set; } //["shack"]
    public IEnumerable<string>? entered_zones { get; set; } //["laundry", "shack"]
    public bool has_clip { get; set; }
    public bool has_snapshot { get; set; }
}