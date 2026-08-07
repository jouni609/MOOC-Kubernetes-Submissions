```powershell
.\install-knative.ps1

kubectl apply -f manifests/namespace.yaml
kubectl apply -f manifests/hello.yaml
kubectl apply -f manifests/autoscaling.yaml
kubectl apply -f manifests/traffic-v1.yaml
kubectl apply -f manifests/traffic-v2.yaml
kubectl apply -f manifests/traffic-split.yaml

kubectl get ksvc --namespace exercises

curl.exe -H "Host: hello.exercises.<YOUR_IP>.sslip.io" http://localhost:8081

1..20 | ForEach-Object {
  curl.exe --silent -H "Host: traffic-example.exercises.<YOUR_IP>.sslip.io" http://localhost:8081
}
```
