$ErrorActionPreference = "Continue"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path

kubectl label namespace default istio.io/use-waypoint- 2>$null
istioctl waypoint delete --all 2>$null
kubectl label namespace default istio.io/dataplane-mode- 2>$null

kubectl delete httproute reviews -n default --ignore-not-found
kubectl delete authorizationpolicy productpage-ztunnel productpage-waypoint -n default --ignore-not-found
kubectl delete -f (Join-Path $Root "samples\curl\curl.yaml") --ignore-not-found
kubectl delete -f (Join-Path $Root "samples\bookinfo\bookinfo.yaml") --ignore-not-found
kubectl delete -f (Join-Path $Root "samples\bookinfo\bookinfo-versions.yaml") --ignore-not-found
kubectl delete -f (Join-Path $Root "samples\bookinfo\bookinfo-gateway.yaml") --ignore-not-found
kubectl delete -f (Join-Path $Root "samples\addons\kiali.yaml") --ignore-not-found

istioctl uninstall -y --purge
kubectl delete namespace istio-system --ignore-not-found
