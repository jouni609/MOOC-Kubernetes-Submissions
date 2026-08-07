$ErrorActionPreference = "Stop"

$clusterName = "k3s-default"
$k3sVersion = "v1.35.5-k3s1"
$knativeVersion = "knative-v1.22.1"

k3d cluster create $clusterName `
  --port "8082:30080@agent:0" `
  -p "8081:80@loadbalancer" `
  --agents 2 `
  --k3s-arg "--disable=traefik@server:0" `
  --image "rancher/k3s:$k3sVersion"

kubectl apply -f "https://github.com/knative/serving/releases/download/$knativeVersion/serving-crds.yaml"
kubectl apply -f "https://github.com/knative/serving/releases/download/$knativeVersion/serving-core.yaml"
kubectl apply -f "https://github.com/knative-extensions/net-kourier/releases/download/$knativeVersion/kourier.yaml"

kubectl patch configmap config-network `
  --namespace knative-serving `
  --type merge `
  --patch '{"data":{"ingress-class":"kourier.ingress.networking.knative.dev"}}'

kubectl apply -f "https://github.com/knative/serving/releases/download/$knativeVersion/serving-default-domain.yaml"
