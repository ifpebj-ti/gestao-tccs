# Etapa 1: Build da aplicação
FROM node:22-alpine AS builder

# Define diretório de trabalho
WORKDIR /app

# Copia os arquivos de dependência primeiro para aproveitar o cache
COPY package*.json ./

# Instala as dependências
RUN npm install

# Copia o restante da aplicação
COPY . ./

ARG NEXT_PUBLIC_API_URL
ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL

# Compila o projeto Next.js para produção
RUN npm run build

# Etapa 2: Imagem leve para rodar a aplicação
FROM node:22-alpine AS runner

# Define diretório de trabalho
WORKDIR /app

# Copia apenas arquivos essenciais do build anterior
COPY --from=builder /app/package.json ./
COPY --from=builder /app/.next ./.next
COPY --from=builder /app/public ./public
COPY --from=builder /app/node_modules ./node_modules

# Comando padrão ao iniciar o container
CMD ["npm", "start"]
