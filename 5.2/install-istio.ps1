$ErrorActionPreference = "Stop"

function Assert-Command($Name) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command not found: $Name"
    }
}

Assert-Command kubectl
Assert-Command istioctl

kubectl config current-context
kubectl get nodes
istioctl version

$crd = kubectl get crd gateways.gateway.networking.k8s.io 2>$null
if ($LASTEXITCODE -ne 0 -or -not $crd) {
    kubectl apply --server-side -f https://github.com/kubernetes-sigs/gateway-api/releases/download/v1.5.1/experimental-install.yaml
}

istioctl install --set profile=ambient --set values.global.platform=k3d --skip-confirmation
if ($LASTEXITCODE -ne 0) { throw "istioctl install failed" }

kubectl wait --for=condition=Available deployment/istiod -n istio-system --timeout=180s
kubectl rollout status daemonset/istio-cni-node -n istio-system --timeout=180s
kubectl rollout status daemonset/ztunnel -n istio-system --timeout=180s
kubectl get pods -n istio-system -o wide
