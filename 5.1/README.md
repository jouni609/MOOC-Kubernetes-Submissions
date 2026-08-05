# 5.1 DummySite

CRD + controller that mirrors a website into the cluster.

| Piece | Path |
|---|---|
| CRD (`k8s.exercise/v1`) | `crd/dummysite-crd.yaml` |
| C# controller | `controller/` |
| TypeScript 7 site app | `site/` |
| Example CR | `examples/dummysite-example.yaml` |

## Prerequisites

- Docker, k3d (or any cluster), `kubectl`
- .NET 10 SDK (local controller build only)
- Node 20+ (local site build only)

## Build images

```powershell
docker build -t dummysite-app:5.1 5.1/site
docker build -t dummysite-controller:5.1 5.1/controller

# k3d
k3d image import dummysite-app:5.1 dummysite-controller:5.1 -c k3s-default
```

## Apply (course workflow)

```powershell
kubectl apply -f 5.1/crd/dummysite-crd.yaml
kubectl apply -f 5.1/controller/manifests/rbac.yaml
kubectl apply -f 5.1/controller/manifests/deployment.yaml
kubectl apply -f 5.1/examples/dummysite-example.yaml
```

## Verify

```powershell
kubectl get dummysites
kubectl get deploy,svc -l dummysite=example-site
kubectl logs deploy/dummysite-controller
kubectl port-forward svc/dummysite-example-site 8080:80
# open http://localhost:8080  → copy of https://example.com/
```

## Local app builds (optional)

```powershell
# site (TypeScript 7)
cd 5.1/site
npm install
npm run build
$env:WEBSITE_URL="https://example.com/"; npm start

# controller
cd 5.1/controller
dotnet build
```
