using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MiniTwit.Models.DataModels
{
    [Table("follower")]
    [Keyless]
    public class Follower : IValidatableObject
    {
        [Column("who_id")]
        public int WhoId { get; set; }

        [Column("whom_id")]
        public int WhomId { get; set; }



        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (WhoId == WhomId)
            {
                yield return new ValidationResult("You can't follow yourself", [nameof(WhoId)]);
            }
        }

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
