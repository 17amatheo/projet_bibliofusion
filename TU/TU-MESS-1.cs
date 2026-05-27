using System;
using System.Net;
using System.Net.Mail;
using System.Threading; 
using NUnit.Framework;
using Moq;

namespace Package1
{
    // =========================================================================
    // POINT D'ENTRÉE : Exécution pas à pas dans la console (.exe)
    // =========================================================================
    public class Program
    {
        public static void Main(string[] args)
        {

            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=======================================================================");
            Console.WriteLine(" FICHETEST ID  : TU-MESS-1                                            ");
            Console.WriteLine(" NOM DU MODULE : Envoie d'e-mails via SMTP                            ");
            Console.WriteLine(" AUTEUR        : AUBRY Mathéo | Date : 27/05/2026 | Version : 1.0     ");
            Console.WriteLine("=======================================================================\n");
            Console.ResetColor();

            Thread.Sleep(1500); // Pause de 1.5 seconde pour lire 

            // 1. Collecte des entrées utilisateurs
            Console.WriteLine("\n[Étape 1] Configuration des données d'entrée...");

            Console.Write("-> Entrez l'adresse de l'expediteur (laisser vide pour tester le cas d'échec) : ");
            string expediteur = Console.ReadLine();

            Console.Write("-> Entrez l'adresse du destinataire (laisser vide pour tester le cas d'échec) : ");
            string destinataire = Console.ReadLine();

            Console.Write("-> Entrez l'objet du message : ");
            string objet = Console.ReadLine();

            Console.Write("-> Entrez le corps du message : ");
            string corps = Console.ReadLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n[Étape 2] Initialisation du Mock SMTP (Interception des appels réseau)...");
            Thread.Sleep(2000); // Pause visuelle

            // 2. Configuration de Moq pour l'exécutable
            var mockSmtpClient = new Mock<ISmtpClient>();

            // Le destinataire est valide
            mockSmtpClient.Setup(s => s.Send(It.IsAny<MailMessage>())).Verifiable();

            // 3. Instanciation de la classe métier avec injection du Mock
            messagerie messager = new messagerie(
                "smtp.test.com",
                "user_test",
                "mdp_test",
                expediteur,
                587,
                (host, p) => mockSmtpClient.Object
            );
            Console.WriteLine("-> Instance de la classe 'messagerie' créée avec succès.");
            Console.ResetColor(); 
            Thread.Sleep(2000);

            // 4. Lancement de la procédure d'envoi
            Console.WriteLine("\n[Étape 3] Appel de la méthode envoyer(). Envoi en cours...");
            Console.Write("[");
            for (int i = 0; i < 10; i++)
            {
                Console.Write("■");
                Thread.Sleep(3000 / 10); // Simulation visuelle d'un chargement de 3 secondes
            }
            Console.WriteLine("] Terminé !");

            bool resultat = messager.envoyer(destinataire, objet, corps);
            Thread.Sleep(1500);

            // 5. Affichage du résultat final (True ou False)
            Console.WriteLine("\n=========================================================");
            if (resultat)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($" RÉSULTAT OBTENU : {resultat} (Succès - Le mail est valide)");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($" RÉSULTAT OBTENU : {resultat} (Échec - Destinataire ou expéditeur invalide ou vide)");
            }
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=========================================================");
            Console.ResetColor();

            Console.WriteLine("\nLe processus est terminé. Appuyez sur une touche pour quitter l'exécutable...");
            Console.ReadKey();
        }
    }

    // =========================================================================
    // INTERFACES ET WRAPPERS POUR LE MOCKING
    // =========================================================================
    public interface ISmtpClient : IDisposable
    {
        ICredentialsByHost Credentials { get; set; }
        bool EnableSsl { get; set; }
        void Send(MailMessage message);
    }

    public class SmtpClientWrapper : SmtpClient, ISmtpClient
    {
        public SmtpClientWrapper(string host, int port) : base(host, port) { }
    }

    // =========================================================================
    // CLASSE MÉTIER 
    // =========================================================================
    public class messagerie
    {
        private string expediteur;
        private string mot_de_passe;
        private string serveurSMTP;
        private string utilisateur;
        private int port;

        private readonly Func<string, int, ISmtpClient> _smtpClientFactory;
        public object m_Systbiblio { get; set; }

        public messagerie(string _serveurSMTP, string _utilisateur, string _mot_de_passe, string _expediteur, int _port)
            : this(_serveurSMTP, _utilisateur, _mot_de_passe, _expediteur, _port, (host, p) => new SmtpClientWrapper(host, p))
        {
        }

        internal messagerie(string _serveurSMTP, string _utilisateur, string _mot_de_passe, string _expediteur, int _port, Func<string, int, ISmtpClient> smtpClientFactory)
        {
            this.serveurSMTP = _serveurSMTP;
            this.utilisateur = _utilisateur;
            this.mot_de_passe = _mot_de_passe;
            this.expediteur = _expediteur;
            this.port = _port;
            this._smtpClientFactory = smtpClientFactory;
        }

        public bool envoyer(string _destinataire, string _objet, string _message)
        {
            try
            {
                // Gestion de l'erreur provoquée volontairement si l'input est vide
                if (string.IsNullOrWhiteSpace(_destinataire)) return false;

                using (MailMessage messageMail = new MailMessage())
                {
                    messageMail.From = new MailAddress(this.expediteur);
                    messageMail.To.Add(_destinataire);
                    messageMail.Subject = _objet;
                    messageMail.Body = _message;

                    using (ISmtpClient smtp = _smtpClientFactory(this.serveurSMTP, this.port))
                    {
                        smtp.Credentials = new NetworkCredential(this.utilisateur, this.mot_de_passe);
                        smtp.EnableSsl = true;
                        smtp.Send(messageMail);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("\nErreur d'envoi SMTP détectée : " + ex.Message);
                return false;
            }
        }
    }

    // =========================================================================
    // BLOC DE TESTS UNITAIRES (Toujours fonctionnel dans l'Explorateur)
    // =========================================================================
    [TestFixture]
    public class MessagerieTests
    {
        private string expediteurValide = "expediteur@test.com";
        private string destinataireValide = "destinataire@test.com";
        private string objetTest = "Objet de test";
        private string messageTest = "Message de test";
        private string serveurSmtp = "smtp.test.com";
        private string utilisateur = "user_test";
        private string motDePasse = "mdp_test";
        private int port = 587;

        [Test]
        public void TU_MESS_1_CasPassant_RetourneTrue()
        {
            var mockSmtpClient = new Mock<ISmtpClient>();
            mockSmtpClient.Setup(s => s.Send(It.IsAny<MailMessage>())).Verifiable();

            var messagerieService = new messagerie(
                serveurSmtp, utilisateur, motDePasse, expediteurValide, port,
                (host, p) => mockSmtpClient.Object
            );

            bool resultat = messagerieService.envoyer(destinataireValide, objetTest, messageTest);

            Assert.That(resultat, Is.True);
            mockSmtpClient.Verify(s => s.Send(It.IsAny<MailMessage>()), Times.Once);
        }

        [Test]
        public void TU_MESS_1_CasErreur_RetourneFalse()
        {
            var mockSmtpClient = new Mock<ISmtpClient>();
            mockSmtpClient.Setup(s => s.Send(It.IsAny<MailMessage>()))
                          .Throws(new SmtpException("Erreur simulée"));

            var messagerieService = new messagerie(
                serveurSmtp, utilisateur, motDePasse, expediteurValide, port,
                (host, p) => mockSmtpClient.Object
            );

            bool resultat = messagerieService.envoyer(destinataireValide, objetTest, messageTest);

            Assert.That(resultat, Is.False);
        }
    }
}
