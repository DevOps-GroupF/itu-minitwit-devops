using System.ComponentModel.DataAnnotations.Schema;

/* drop table if exists message; */
/* create table message ( */
/*   message_id integer primary key autoincrement, */
/*   author_id integer not null, */
/*   text string not null, */
/*   pub_date integer, */
/*   flagged integer */
/* ); */


namespace MiniTwit.Models
{
    [Table("message")]
    public class Twit
    {
        [Column("message_id")]
        public int Id { get; set; }

        [Column("author_id")]
        public int AuthorId { get; set; }

        [Column("text")]
        public string Text { get; set; }

        [Column("pub_date")]
        public int PubDate { get; set; }

        [Column("flagged")]
        public int Flagged { get; set; }
    }
}
