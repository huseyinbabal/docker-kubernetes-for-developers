resource "hcloud_ssh_key" "k8s" {
  name       = "k8s-ssh-key"
  public_key = chomp(file(pathexpand(var.ssh_public_key_path)))
}
