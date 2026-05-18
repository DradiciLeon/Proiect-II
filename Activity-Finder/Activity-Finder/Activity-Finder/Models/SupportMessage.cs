using System;

namespace Activity_Finder.Models
{
    public class SupportMessage
    {
        public int Id { get; set; }

        // Cheia străină către tabela Users
        public int UserId { get; set; }

        public string Message { get; set; }
        public DateTime SentAt { get; set; }

        public bool IsSolved { get; set; }

        public string AdminReply { get; set; }

        // LIPSESC ACESTE LINII LA TINE (Asta rezolvă eroarea):
        // Aceasta este "proprietatea de navigare" care îi spune SQL-ului că UserId-ul de mai sus aparține unui User.
        public virtual User User { get; set; }
    }
}