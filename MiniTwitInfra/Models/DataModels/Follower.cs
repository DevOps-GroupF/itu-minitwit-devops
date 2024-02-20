using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MiniTwitInfra.Models.DataModels
{
    [Table("follower")]
    [Keyless]
    public class Follower
    {
        [Column("who_id")]
        public int WhoId { get; set; }

        [Column("whom_id")]
        public int WhomId { get; set; }

        public static async Task<bool> DoesFollowerExistAsync(
            int whoId,
            int whomId,
            Data.MiniTwitContext miniTwitContext
        )
        {
            Follower follower = await miniTwitContext.Followers.FirstOrDefaultAsync(f =>
                f.WhoId == whoId && f.WhomId == whomId
            );

            return follower != null;
        }
    }
}
