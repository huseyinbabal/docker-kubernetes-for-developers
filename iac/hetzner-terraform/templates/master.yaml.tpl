#cloud-config
package_update: true

write_files:
  - path: /etc/modules-load.d/kubernetes.conf
    content: |
      overlay
      br_netfilter

  - path: /etc/sysctl.d/kubernetes.conf
    content: |
      net.bridge.bridge-nf-call-ip6tables = 1
      net.bridge.bridge-nf-call-iptables = 1
      net.ipv4.ip_forward = 1

runcmd:
  # --- Kernel modules ---
  - modprobe overlay
  - modprobe br_netfilter
  - sysctl --system

  # --- Disable swap ---
  - swapoff -a
  - sed -i '/swap/d' /etc/fstab

  # --- Container runtime (containerd via docker.io) ---
  - apt-get update && apt-get install -y docker.io
  - mkdir -p /etc/containerd
  - containerd config default > /etc/containerd/config.toml
  - sed -i 's/ SystemdCgroup = false/ SystemdCgroup = true/' /etc/containerd/config.toml
  - systemctl restart containerd

  # --- Kubernetes packages ---
  - apt-get install -y apt-transport-https ca-certificates curl gpg
  - mkdir -p -m 755 /etc/apt/keyrings
  - curl -fsSL https://pkgs.k8s.io/core:/stable:/v${kubernetes_version}/deb/Release.key | gpg --dearmor -o /etc/apt/keyrings/kubernetes-apt-keyring.gpg
  - echo 'deb [signed-by=/etc/apt/keyrings/kubernetes-apt-keyring.gpg] https://pkgs.k8s.io/core:/stable:/v${kubernetes_version}/deb/ /' | tee /etc/apt/sources.list.d/kubernetes.list
  - apt-get update
  - apt-get install -y kubelet kubeadm kubectl
  - apt-mark hold kubelet kubeadm kubectl
  - echo "KUBELET_EXTRA_ARGS=--node-ip=$(hostname -I | awk '{print $2}')" > /etc/default/kubelet
  - systemctl enable --now kubelet

  # --- Initialize control plane ---
  - kubeadm init --pod-network-cidr=${pod_network_cidr} --apiserver-advertise-address=$(hostname -I | awk '{print $2}') --apiserver-cert-extra-sans=$(hostname -I | awk '{print $1}')

  # --- kubeconfig for root ---
  - mkdir -p /root/.kube
  - cp /etc/kubernetes/admin.conf /root/.kube/config

  # --- Install Calico CNI ---
  - kubectl --kubeconfig=/root/.kube/config apply -f https://raw.githubusercontent.com/projectcalico/calico/v3.28.0/manifests/calico.yaml

  # --- Save join command for workers ---
  - kubeadm token create --print-join-command > /root/kubeadm_join_cmd.sh
