# 5.2 Istio Ambient

| Path | Role |
|---|---|
| `install-istio.ps1` | Ambient install (`platform=k3d`) |
| `run-getting-started.ps1` | Bookinfo, Kiali, auth, traffic split |
| `cleanup.ps1` | Remove sample + Istio |
| `samples/` | Bookinfo, curl, Kiali |
| `manifests/` | AuthorizationPolicy, HTTPRoute |

Host Prometheus/Grafana (`localhost:9090` / `:3000`). Kiali uses `host.docker.internal`.

```powershell
.\install-istio.ps1
.\run-getting-started.ps1
kubectl port-forward svc/bookinfo-gateway-istio 8080:80
istioctl dashboard kiali
.\cleanup.ps1
```

## License

Redistributed Istio samples: Apache-2.0 — see `LICENSE` and `NOTICE`.
