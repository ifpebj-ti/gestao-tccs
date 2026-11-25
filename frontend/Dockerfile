# Etapa 1: Build da aplicação
FROM node:26-alpine AS builder

# Define variável de ambiente para a versão da aplicação
ARG NEXT_PUBLIC_APP_VERSION
ENV NEXT_PUBLIC_APP_VERSION=$NEXT_PUBLIC_APP_VERSION

# URL da API — precisa estar disponível em build time para o Next.js embutir no bundle
ARG NEXT_PUBLIC_API_URL
ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL

# Define diretório de trabalho
WORKDIR /app

# Copia os arquivos de dependência primeiro para aproveitar o cache
COPY package*.json ./
COPY scripts/minimatch-compat.cjs ./scripts/minimatch-compat.cjs

# Instala as dependências
RUN npm ci

# Copia o restante da aplicação
COPY . ./

# Compila o projeto Next.js para produção
RUN npm run build

# Etapa 2: Imagem leve para rodar a aplicação
FROM node:26-alpine AS runner

# Define diretório de trabalho
WORKDIR /app

COPY --from=builder /app/package*.json ./
COPY --from=builder /app/scripts/minimatch-compat.cjs ./scripts/minimatch-compat.cjs
RUN npm ci --omit=dev && npm uninstall --global npm

# Copia apenas arquivos essenciais do build anterior
COPY --from=builder /app/.next ./.next
COPY --from=builder /app/public ./public

RUN chown -R node:node /app/.next
USER node
# Comando padrão ao iniciar o container
CMD ["node", "node_modules/next/dist/bin/next", "start"]
