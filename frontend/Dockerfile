# Etapa 1: Build da aplicação
FROM node:18-alpine AS builder

# Define diretório de trabalho
WORKDIR /app

# Copia os arquivos de dependência primeiro para aproveitar o cache
COPY package*.json ./

# Instala as dependências
RUN npm install

# Copia o restante da aplicação
COPY . ./

# Permite passar qual env usar no build
ARG ENV_FILE

# Copia o env correto para .env (Next lê automaticamente)
RUN cp $ENV_FILE .env

# Compila o projeto Next.js para produção
RUN npm run build

# Etapa 2: Imagem leve para rodar a aplicação
FROM node:18-alpine AS runner

# Define diretório de trabalho
WORKDIR /app

# Copia apenas arquivos essenciais do build anterior
COPY --from=builder /app/package.json ./
COPY --from=builder /app/.next ./.next
COPY --from=builder /app/public ./public
COPY --from=builder /app/node_modules ./node_modules

# Comando padrão ao iniciar o container
CMD ["npm", "start"]
