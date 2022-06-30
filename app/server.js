const express = require('express');
const fetch = require('node-fetch');

const path = require('path');

const port = process.env.PORT || 4567;
const app = express();

//index.html caches aldri. Bundle caches i 5 dager hvis de ikke er endret
app.use(
  express.static('build', {
    etag: true, // Just being explicit about the default.
    lastModified: true, // Just being explicit about the default.
    setHeaders: (res, path) => {
      const hashRegExp = new RegExp('\\.[0-9a-f]{8}\\.');
      if (path.endsWith('.html')) {
        res.setHeader('Cache-Control', 'no-cache');
      } else if (hashRegExp.test(path)) {
        res.setHeader('Cache-Control', 'max-age=432000');
      }
    },
  })
);

app.use('/fonts', express.static('fonts', { maxAge: '5d' }));

app.get('/health', (request, response) => {
  response.send('healthy');
});

function getArraSvcUrl() {
  return (
    process.env.ARRANGEMENT_SVC_URL || 'https://api.dev.bekk.no/arrangement-svc'
  );
}

app.get('/config', (request, response) =>
  response.send({
    bekkApiUrl: process.env.BEKK_API_URL || 'https://api.dev.bekk.no',
    arrangementSvcUrl: getArraSvcUrl(),
    employeeSvcUrl:
      process.env.EMPLOYEE_SVC_URL || 'https://api.dev.bekk.no/employee-svc',
    audience: process.env.AUTH0_AUDIENCE || 'QHQy75S7tmnhDdBGYSnszzlhMPul0fAE',
    issuerDomain: process.env.AUTH0_ISSUER_DOMAIN || 'bekk-dev.eu.auth0.com',
    scopes: process.env.SCOPES || 'openid name groups email',
  })
);

app.get('*', async (request, response) => {
  if (startsWith(request.headers['user-agent'], 'Slackbot-LinkExpanding')) {
    try {
      const path = encodeURIComponent(request.path);
      const res = await fetch(`${getArraSvcUrl()}/events/${path}/unfurl`);
      if (res.status > 299) {
        throw res.status;
      }
      const event = await res.json();

      response.send(html(event));
    } catch (statusCode) {
      response.sendStatus(statusCode);
    }
  } else {
    response.sendFile(path.join(__dirname, 'build/index.html'));
  }
});

app.listen(port);
console.log(`Server started on port ${port}`);

// UNFURL

function startsWith(str, word) {
  return str.lastIndexOf(word, 0) === 0;
}

function html({
  event: {
    title,
    description,
    location,
    organizerName,
    startDate,
    openForRegistrationTime,
    closeRegistrationTime,
    maxParticipants = Infinity,
  },
  numberOfParticipants,
}) {
  const availableSpots = maxParticipants - numberOfParticipants;
  const opens = new Date(Number(openForRegistrationTime));
  const closes = new Date(Number(closeRegistrationTime));
  const isAlreadyOpen = opens < new Date();
  const hasClosed = closeRegistrationTime !== undefined && closes < new Date();

  const twoDigits = (n) => {
    return n.toString().padStart(2, '0');
  };
  const truncateText = (n, s) => {
    if (s.length > n) {
      return `${s.substring(0, n - 3).trim()}...`;
    }
    return s;
  };
  const formatDateTime = ({ date, time }) => {
    return `${twoDigits(date.day)}.${twoDigits(date.month)}.${twoDigits(
      date.year % 2000
    )} ${twoDigits(time.hour)}:${twoDigits(time.minute)}`;
  };
  return `
    <!DOCTYPE html>
    <html>
      <head>

        <meta property="og:type" content="website" />
        <meta property="og:site_name" content="Skjer" />
        <meta property="og:url" content="https://skjer.bekk.no/" />
        <meta property="og:title" content="${sanitize(title)}" />
        <meta property="og:description" content="${truncateText(
          256,
          sanitize(description)
        )}" />

        <meta name="twitter:domain" value="skjer.bekk.no" />
        <meta name="twitter:title" value="${sanitize(title)}" />
        <meta name="twitter:description" value="${sanitize(description)}" />
        <meta name="twitter:url" value="https://skjer.bekk.no/" />

        <meta name="twitter:label1" value="Finner sted" />
        <meta name="twitter:data1" value="${
          sanitize(location) + `, ${formatDateTime(startDate)}`
        }" />

        ${
          !isAlreadyOpen
            ? `
        <meta name="twitter:label2" value="Påmelding åpner" />
        <meta name="twitter:data2" value="${formatDateTime({
          date: {
            day: opens.getDate(),
            month: opens.getMonth() + 1,
            year: opens.getFullYear(),
          },
          time: { hour: opens.getHours(), minute: opens.getMinutes() },
        })}" />`
            : hasClosed
            ? `
        <meta name="twitter:label2" value="Påmelding er stengt" />
        <meta name="twitter:data2" value="Påmeldinger har dessverre stengt" />`
            : `
        <meta name="twitter:label2" value="Påmelding er åpen!" />
        <meta name="twitter:data2" value="${
          availableSpots === Infinity
            ? 'Ubegrenset plasser'
            : 'Det er begrenset plasser'
        }" />`
        }

      </head>
    </html>
  `;
}

function sanitize(s) {
  return s
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/&/g, '&amp;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&apos;');
}
