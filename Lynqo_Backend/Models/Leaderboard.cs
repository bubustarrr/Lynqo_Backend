using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lynqo_Backend.Models
{
    [Table("leaderboards")]
    public class Leaderboard
    {
        [Key][Column("id")]
        public int Id { get; set; }
        [Column("league_name")]
        public string LeagueName { get; set; }
        [Column("start_date")]
        public DateTime? StartDate { get; set; }
        [Column("end_date")]
        public DateTime? EndDate { get; set; }
    }
}
