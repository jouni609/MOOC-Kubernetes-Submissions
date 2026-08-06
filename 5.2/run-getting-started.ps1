$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path

function Assert-Command($Name) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command not found: $Name"
    }
}

Assert-Command kubectl
Assert-Command istioctl

kubectl apply -f (Join-Path $Root "samples\bookinfo\bookinfo.yaml")
kubectl apply -f (Join-Path $Root "samples\bookinfo\bookinfo-versions.yaml")
kubectl apply -f (Join-Path $Root "samples\bookinfo\bookinfo-gateway.yaml")
kubectl annotate gateway bookinfo-gateway networking.istio.io/service-type=ClusterIP --namespace=default --overwrite

@("details-v1", "ratings-v1", "reviews-v1", "reviews-v2", "reviews-v3", "productpage-v1") | ForEach-Object {
    kubectl rollout status "deployment/$_" -n default --timeout=180s
}

$deadline = (Get-Date).AddMinutes(2)
do {
    $programmed = kubectl get gateway bookinfo-gateway -n default -o jsonpath='{.status.conditions[?(@.type=="Programmed")].status}' 2>$null
    if ($programmed -eq "True") { break }
    Start-Sleep 2
} while ((Get-Date) -lt $deadline)
kubectl get gateway bookinfo-gateway -n default

kubectl label namespace default istio.io/dataplane-mode=ambient --overwrite

kubectl apply -f (Join-Path $Root "samples\addons\kiali.yaml")
kubectl rollout status deployment/kiali -n istio-system --timeout=180s

@'
apiVersion: security.istio.io/v1
kind: AuthorizationPolicy
metadata:
  name: productpage-ztunnel
  namespace: default
spec:
  selector:
    matchLabels:
      app: productpage
  action: ALLOW
  rules:
  - from:
    - source:
        principals:
        - cluster.local/ns/default/sa/bookinfo-gateway-istio
'@ | kubectl apply -f -

kubectl apply -f (Join-Path $Root "samples\curl\curl.yaml")
kubectl rollout status deployment/curl -n default --timeout=180s
kubectl exec deploy/curl -n default -- curl -sS --max-time 5 http://productpage:9080/productpage 2>&1 | Out-Host
Write-Host "curl L4 exit: $LASTEXITCODE"

istioctl waypoint apply --enroll-namespace --wait
kubectl apply -f (Join-Path $Root "manifests\productpage-waypoint.yaml")
kubectl apply -f (Join-Path $Root "manifests\productpage-ztunnel.yaml")
Start-Sleep 3

kubectl exec deploy/curl -n default -- curl -sS --max-time 5 http://productpage:9080/productpage -X DELETE 2>&1 | Out-Host
kubectl exec deploy/reviews-v1 -n default -- curl -sS --max-time 5 http://productpage:9080/productpage 2>&1 | Out-Host
kubectl exec deploy/curl -n default -- curl -sS --max-time 10 http://productpage:9080/productpage 2>&1 |
    Select-String -Pattern '<title>.*</title>' | Out-Host

kubectl apply -f (Join-Path $Root "manifests\reviews-split.yaml")
Start-Sleep 2
$out = kubectl exec deploy/curl -n default -- sh -c 'for i in $(seq 1 100); do curl -s http://productpage:9080/productpage | grep -o "reviews-v[0-9]"; done' 2>&1
$lines = @($out -split "`n" | Where-Object { $_ -match "reviews-v" })
$v1 = @($lines | Where-Object { $_ -match "reviews-v1" }).Count
$v2 = @($lines | Where-Object { $_ -match "reviews-v2" }).Count
$v3 = @($lines | Where-Object { $_ -match "reviews-v3" }).Count
Write-Host "reviews-v1=$v1 reviews-v2=$v2 reviews-v3=$v3"
