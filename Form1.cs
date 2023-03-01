using System;
using System.Windows.Forms;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Management;
using System.Security.AccessControl;

namespace AppSelb
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                string userName = textBox1.Text;
                string password = textBox2.Text;
                string description = "Usuário usado para o scanner";
                string fullName = "Scanner Usuário";

                using (DirectoryEntry dirEntry = new DirectoryEntry("WinNT://localhost"))
                {
                    using (DirectoryEntry newUser = dirEntry.Children.Add(userName, "user"))
                    {
                        newUser.Invoke("SetPassword", new object[] { password });
                        newUser.Invoke("Put", new object[] { "UserFlags", 0x10000 }); //ADS_UF_DONT_EXPIRE_PASSWD
                        newUser.Invoke("Put", new object[] { "Description", description });
                        newUser.Invoke("Put", new object[] { "FullName", fullName });
                        newUser.CommitChanges();
                    }
                }

                using (PrincipalContext context = new PrincipalContext(ContextType.Machine))
                {
                    using (UserPrincipal user = UserPrincipal.FindByIdentity(context, userName))
                    {
                        GroupPrincipal group = GroupPrincipal.FindByIdentity(context, "Administradores");
                        if (group != null)
                        {
                            group.Members.Add(user);
                            group.Save();
                        }
                    }
                }

                MessageBox.Show("Usuário criado com sucesso!", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Falha ao criar usuário! {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Caminho da pasta a ser criada
            string folderPath = @"C:\scanner";

            try
            {
                // Cria a pasta se ela ainda não existir
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                    MessageBox.Show("Pasta criada com sucesso.", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("A pasta já existe.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                // Define as permissões de acesso para a pasta
                DirectorySecurity dirSecurity = new DirectorySecurity();
                dirSecurity.AddAccessRule(new FileSystemAccessRule("scanner", FileSystemRights.ReadAndExecute | FileSystemRights.Write, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
                Directory.SetAccessControl(folderPath, dirSecurity);

                // Compartilha a pasta
                ManagementClass managementClass = new ManagementClass("Win32_Share");
                ManagementBaseObject inParams = managementClass.GetMethodParameters("Create");
                inParams["Description"] = "Pasta compartilhada para scanner";
                inParams["Name"] = "scanner";
                inParams["Path"] = folderPath;
                inParams["Type"] = 0x0; // 0x0 = DISK_DRIVE
                ManagementBaseObject outParams = managementClass.InvokeMethod("Create", inParams, null);

                MessageBox.Show("A pasta foi compartilhada com sucesso.", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Atualiza o TextBox3 com o caminho da pasta
                textBox3.Text = $@"\\{Environment.MachineName}\scanner";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao criar a pasta: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
