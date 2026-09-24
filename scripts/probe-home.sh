#!/bin/bash
curl -sI http://127.0.0.1:5080/MOVASSEGHISTORE/images/products/0071/01.webp | head -3
curl -s http://127.0.0.1:5080/MOVASSEGHISTORE/ | grep -o 'src="/[^"]*"' | head -20
curl -sI http://85.133.244.142/images/products/0071/01.webp | head -3
curl -sI http://85.133.244.142/MOVASSEGHISTORE/images/products/0071/01.webp | head -3
