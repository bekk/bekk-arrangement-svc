import http from "k6/http";
import { check } from "k6";
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { randomString } from "https://jslib.k6.io/k6-utils/1.2.0/index.js";

export let options = {
  insecureSkipTLSVerify: true,
  noConnectionReuse: false,
  stages: [
    { duration: "1m", target: 100 },
    { duration: "2m", target: 100 },
    { duration: "1m", target: 0 },
  ],
};

export default function () {
  const token = "";
  const eventId = "aa42634a-cc08-4446-b7d3-2c0489afe561";
  let email = `${randomString(10)}@foo`;
  const params = {
    headers: {
      Authorization: `bearer ${token}`,
      "Content-Type": "application/json",
    },
  };
  const payload = {
    name: randomString(10),
    department: "Teknologi",
    email: {
      email,
    },
      participantAnswers: [
      {
        questionId: "2572",
        eventId,
        email,
        question: "spm one",
        answer: "1",
      },
      {
        questionId: "2573",
        eventId,
        email,
        question: "spm two",
        answer: "2",
      },
      {
        questionId: "2574",
        eventId,
        email,
        question: "spm three",
        answer: "3",
      },
      {
        questionId: "2575",
        eventId,
        email,
        question: "spm four",
        answer: "4",
      },
      {
        questionId: "2576",
        eventId,
        email,
        question: "spm five",
        answer: "4",
      },
    ],
    cancelUrlTemplate: "http://localhost:3000/events/{eventId}/cancel/{email}?cancellationToken=%7BcancellationToken%7D",
    viewUrlTemplate: "http://localhost:3000/events/{eventId}"
  };
  const url = `http://localhost:5000/api/events/${eventId}/participants/${email}`;
  let response = http.post(url, JSON.stringify(payload), params);

  check(response, {
    "has 200 status": (r) => r.status === 200,
  });
}

export function handleSummary(data) {
  return {
    "Spike-RegisterParticpants.html": htmlReport(data),
  };
}
