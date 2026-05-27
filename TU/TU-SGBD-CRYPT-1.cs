using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using MySql.Data.MySqlClient;

namespace Package1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=======================================================================");
            Console.WriteLine(" FICHETEST ID  : TU-SGBD-CRYPT-1                                       ");
            Console.WriteLine(" NOM DU MODULE : Gestion sécurisée des données MySQL                  ");
            Console.WriteLine(" AUTEUR        : AUBRY Mathéo | Date : 27/05/2026 | Version : 1.3     ");
            Console.WriteLine("=======================================================================\n");
            Console.ResetColor();

            string conString = "server=localhost;uid=root;pwd=;database=bibliofusion;";
            string cleEncryption = "clee_test";
            SGBD sgbd = new SGBD();

            try
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Choisissez l'action à réaliser :");
                Console.WriteLine(" [1] Écriture (Saisie de l'email, Vidage Table, Chiffrement, puis Insertion)");
                Console.WriteLine(" [2] Lecture (Récupération brute en BDD, Déchiffrement, puis Comparaison)");
                Console.Write("\nVotre choix (1 ou 2) : ");
                string choix = Console.ReadLine();
                Console.ResetColor();

                if (choix != "1" && choix != "2")
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\nChoix invalide. Fin du test.");
                    return;
                }

                // =================================================================
                // Étape 1 : Ouvrir la connexion MySQL
                // =================================================================
                Console.WriteLine("\n[Étape 1] Ouvrir la connexion MySQL (Serveur XAMPP)...");
                Attendre();
                sgbd.connect("localhost", "root", "", "bibliofusion");
                Attendre();

                if (choix == "1")
                {
                    Console.Write("\n-> Veuillez saisir le paramètre d'entrée (email) : ");
                    string emailOriginal = Console.ReadLine();

                    if (string.IsNullOrWhiteSpace(emailOriginal))
                    {
                        emailOriginal = "adherent@test.com";
                        Console.WriteLine($"[Info] Saisie vide. Utilisation de la valeur par défault : {emailOriginal}");
                    }

                    // =================================================================
                    // Étape 2: Chiffrer les données sensibles avec AES-GCM
                    // =================================================================
                    Console.WriteLine("\n[Étape 2] Chiffrer les données avec AES-GCM...");
                    Attendre();
                    string emailChiffre = Chiffrer(emailOriginal, cleEncryption);

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  Valeur d'entrée : {emailOriginal}");
                    Console.WriteLine($"   Donnée chiffrée  : {emailChiffre}");
                    Console.ResetColor();
                    Attendre();

                    // =================================================================
                    // Étape 3: Nettoyer la base (Bypass FK) et exécuter l'INSERT INTO
                    // =================================================================
                    Console.WriteLine("\n[Étape 3] Réinitialisation et écriture dans la base...");

                    Console.WriteLine("   Vidage de la table Adherents ...");
                    sgbd.ViderTable(conString);
                    Attendre();

                    Console.WriteLine("  Exécution de la requête INSERT INTO...");
                    sgbd.ecrire(conString, emailChiffre);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("  [Résultat] Insertion réussie.");
                    Console.ResetColor();
                }
                else
                {
                    // =================================================================
                    // Étape 4: Lire les données stockées
                    // =================================================================
                    Console.WriteLine("\n[Étape 4] Lire les données stockées depuis MySQL...");
                    Attendre();
                    string emailChiffreLu = sgbd.lire(conString);

                    if (string.IsNullOrEmpty(emailChiffreLu))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("  [Résultat] Erreur : Aucun enregistrement trouvé dans la table Adherents.");
                        Console.ResetColor();
                        return;
                    }

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  [>] Donnée brute récupérée : {emailChiffreLu}");
                    Console.ResetColor();
                    Attendre();

                    // =================================================================
                    // Étape 5: Déchiffrer les données récupérées
                    // =================================================================
                    Console.WriteLine("\n[Étape 5] Déchiffrement des données récupérées...");
                    Attendre();

                    string emailDechiffreLu = string.Empty;
                    bool dechiffrementReussi = false;

                    try
                    {
                        emailDechiffreLu = Dechiffrer(emailChiffreLu, cleEncryption);
                        dechiffrementReussi = true;

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"  [Résultat] Déchiffrement valide. Valeur : {emailDechiffreLu}");
                        Console.ResetColor();
                        Attendre();
                    }
                    catch (Exception)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("  [EXCEPTION attendue] Erreur : Non validité des données restituées (Données corrompues ou modifiées).");
                        Console.ResetColor();
                    }

                }

                Console.WriteLine("\nFermeture de la connexion (deconnect)...");
                Attendre();
                sgbd.deconnect("localhost", "root", "", "bibliofusion");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[ERREUR SGBD] : {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine("\nTravail terminé. Appuyez sur une touche pour quitter...");
            Console.ReadKey();
        }

        private static void Attendre()
        {
            Thread.Sleep(1500);
        }

        private static string Chiffrer(string donnees, string cle)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] nonce = RandomNumberGenerator.GetBytes(12);
            byte[] plaintext = Encoding.UTF8.GetBytes(donnees);

            using var kdf = new Rfc2898DeriveBytes(cle, salt, 100000, HashAlgorithmName.SHA256);
            byte[] key = kdf.GetBytes(32);

            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag = new byte[16];

            using (var aesGcm = new AesGcm(key))
            {
                aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);
            }

            byte[] result = new byte[salt.Length + nonce.Length + tag.Length + ciphertext.Length];
            Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
            Buffer.BlockCopy(nonce, 0, result, salt.Length, nonce.Length);
            Buffer.BlockCopy(tag, 0, result, salt.Length + nonce.Length, tag.Length);
            Buffer.BlockCopy(ciphertext, 0, result, salt.Length + nonce.Length + tag.Length, ciphertext.Length);

            return Convert.ToBase64String(result);
        }

        private static string Dechiffrer(string donneeChiffree, string cle)
        {
            byte[] full = Convert.FromBase64String(donneeChiffree);
            byte[] salt = new byte[16];
            byte[] nonce = new byte[12];
            byte[] tag = new byte[16];
            byte[] ciphertext = new byte[full.Length - (16 + 12 + 16)];

            Buffer.BlockCopy(full, 0, salt, 0, 16);
            Buffer.BlockCopy(full, 16, nonce, 0, 12);
            Buffer.BlockCopy(full, 28, tag, 0, 16);
            Buffer.BlockCopy(full, 44, ciphertext, 0, ciphertext.Length);

            using var kdf = new Rfc2898DeriveBytes(cle, salt, 100000, HashAlgorithmName.SHA256);
            byte[] key = kdf.GetBytes(32);

            byte[] plaintext = new byte[ciphertext.Length];
            using (var aesGcm = new AesGcm(key))
            {
                aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
            }

            return Encoding.UTF8.GetString(plaintext);
        }
    }

    public class SGBD
    {
        public void connect(string nom_server, string identifiant, string mdp, string bdd)
        {
            string cs = $"server={nom_server};uid={identifiant};pwd={mdp};database={bdd};";
            using var con = new MySqlConnection(cs);
            con.Open();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  [SGBD] connect() -> Connexion établie.");
            Console.ResetColor();
        }

        public void deconnect(string nom_server, string identifiant, string mdp, string bdd)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("  [SGBD] deconnect() -> Session déconnectée.");
            Console.ResetColor();
        }

        public void ViderTable(string conString)
        {
            using var con = new MySqlConnection(conString);
            con.Open();

            // 1. Force la désactivation temporaire des contraintes de clés étrangères
            using (var cmdDisable = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 0;", con))
            {
                cmdDisable.ExecuteNonQuery();
            }

            // 2. Vide proprement les enregistrements et réinitialise l'auto-incrémentation
            using (var cmdTruncate = new MySqlCommand("TRUNCATE TABLE Adherents;", con))
            {
                cmdTruncate.ExecuteNonQuery();
            }

            // 3. Réactivation des contraintes relationnelles de la base de données
            using (var cmdEnable = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 1;", con))
            {
                cmdEnable.ExecuteNonQuery();
            }
        }

        public void ecrire(string conString, string emailChiffre)
        {
            using var con = new MySqlConnection(conString);
            con.Open();

            int nouvelId = 1;
            string nomColonneId = "idAdherents";

            try
            {
                using var schemaCmd = new MySqlCommand("SHOW COLUMNS FROM Adherents LIKE 'idAdherents';", con);
                using var reader = schemaCmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    nomColonneId = "id";
                }
            }
            catch { }

            string selectMaxIdQuery = $"SELECT COALESCE(MAX({nomColonneId}), 0) + 1 FROM Adherents;";
            try
            {
                using var maxCmd = new MySqlCommand(selectMaxIdQuery, con);
                nouvelId = Convert.ToInt32(maxCmd.ExecuteScalar());
            }
            catch
            {
                nouvelId = 1; // Valeur par défaut logique si la table vient d'être tronquée
            }

            string queryInsert = $"INSERT INTO Adherents ({nomColonneId}, Email) VALUES (@Id, @Email);";
            using var insertCmd = new MySqlCommand(queryInsert, con);
            insertCmd.Parameters.AddWithValue("@Id", nouvelId);
            insertCmd.Parameters.AddWithValue("@Email", emailChiffre);
            insertCmd.ExecuteNonQuery();
        }

        public string lire(string conString)
        {
            using var con = new MySqlConnection(conString);
            con.Open();

            string query = "SELECT Email FROM Adherents WHERE Email IS NOT NULL ORDER BY 1 DESC LIMIT 1;";
            using var selectCmd = new MySqlCommand(query, con);

            using var reader = selectCmd.ExecuteReader();
            if (reader.Read() && !reader.IsDBNull(0))
            {
                return reader.GetString(0);
            }
            return null;
        }
    }
}
