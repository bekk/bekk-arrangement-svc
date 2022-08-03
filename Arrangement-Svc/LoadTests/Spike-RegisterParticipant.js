import http from "k6/http";
import { check } from "k6"
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import {
  randomString,
} from 'https://jslib.k6.io/k6-utils/1.2.0/index.js';

export let options = {
  insecureSkipTLSVerify: true,
  noConnectionReuse: false,
  stages: [
    { duration: '1m', target: 100 },
    { duration: '2m', target: 100},
    { duration: '1m', target: 0 },
  ]
}

export default function() {
  const token = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6Ik56RTVOVFJHUVRnNVJVRkNSVFJEUkRnelEwUXdORE5GUkRZeU4wWkZNVEJGUmpCRFFUVkJRUSJ9.eyJodHRwczovL2FwaS5iZWtrLm5vL2NsYWltcy9wZXJtaXNzaW9uIjpbInJlYWQ6c3RhZmZpbmciLCJ3cml0ZTpzdGFmZmluZyIsIndyaXRlOnRlbnRhdGl2U3RhZmZpbmciLCJyZWFkOnRlbnRhdGl2U3RhZmZpbmciLCJyZWFkOmVtcGxveWVlcyIsInJlYWQ6YmVrayIsIndyaXRlOmVtcGxveWVlcyIsIndyaXRlOnRpbWVjb2RlcyIsIndyaXRlOmFjY291bnRpbmciLCJhZG1pbjppbnZvaWNlLXN2YyIsImFkbWluOlNhbGVzQW5kUHJvamVjdHMtc3ZjIiwicmVhZDpvcHBvcnR1bml0eSIsIndyaXRlOm9wcG9ydHVuaXR5Iiwid3JpdGU6cHJvamVjdCIsInJlYWRXcml0ZTpwcm9nbm9zaXMiLCJyZWFkV3JpdGU6c3ViY29udHJhY3RvciIsImFkbWluOnRpbWVrZWVwZXItc3ZjIiwicmVhZDplZ2VubWVsZGluZ2VyIiwicmVhZDp0aW1lY29kZXMiLCJyZWFkV3JpdGU6dGltZXNoZWV0cyIsInJlYWQ6aW52b2ljZXMiLCJyZWFkOmN1c3RvbWVyIiwicmVhZDpwcm9qZWN0IiwicmVhZFdyaXRlOmNhbGVuZGFyIiwiYmF0Y2hVcGRhdGU6Y2FsZW5kYXIiLCJyZWFkV3JpdGU6cGFya2luZyIsInJlYWRXcml0ZTpldmVudHMiLCJ3cml0ZTpwcmFjdGljZUdyb3VwcyIsInJlYWQ6Y3YiLCJkZWxldGU6YXV0aDB1c2VyIiwiYWRtaW46Y2FiaW4iLCJyZWFkOmZvcnNpZGUiLCJyZWFkOmNhYmluIiwiYWRtaW46YXV0aCIsImFkbWluOmVtcGxveWVlLXN2YyIsImFkbWluOmFycmFuZ2VtZW50IiwicmVhZDphcnJhbmdlbWVudCIsImFkbWluOnBhcmtpbmciLCJhZG1pbjpzcGlyaXRmb25kIiwicmVhZDphY2NvdW50aW5nIiwiYWRtaW46Y2FsZW5kYXIiLCJyZWFkOlN1YkNvbnRyYWN0b3JSZXBvcnQiLCJhZG1pbjpiZWtrYm9rIiwiYWRtaW46YXRsYXMiLCJhZG1pbjpzdGFmZmluZyJdLCJodHRwczovL2FwaS5iZWtrLm5vL2NsYWltcy9lbXBsb3llZUlkIjoxNDM3LCJuYW1lIjoiQmrDuHJuLUl2YXIgU3Ryw7htIiwiZW1haWwiOiJiam9ybi5pdmFyLnN0cm9tQGJla2subm8iLCJlbWFpbF92ZXJpZmllZCI6dHJ1ZSwiaXNzIjoiaHR0cHM6Ly9iZWtrLWRldi5ldS5hdXRoMC5jb20vIiwic3ViIjoid2FhZHxMT0FIbFBSbEJEd2JMdnhSVHdjdVNOY3FfZFkwZlBpNnJaWk1VREJTYTB3IiwiYXVkIjoiUUhReTc1Uzd0bW5oRGRCR1lTbnN6emxoTVB1bDBmQUUiLCJpYXQiOjE2NTcxNzgwMDcsImV4cCI6MTY1NzIxNDAwN30.Yl5MLgEbJYjhCyrzKeQmeEX_4usANvCgDpKIMXjYIV44rhBqs3xgYDXB2sW7Vw4yZjLz64FL7xf5Nc-knqoaRDiCrwtP-RNKa6HpSQVvw58yhImJph0t3zcNOR4FJMRQylNQomLBK2XCS5WHK1UTwkjkvT6dSvFnvn4Eg_sU_1-9WmzgiWafqvH5OcIZuRUYPjOoui0ib1ABYol_W--__I2HAE5P5VqI0_0r4s5XsEpt81fXgqa_bLVWgyDHLamRqJ9xk-FavfXNIxRaTjUU0d33nnrHkRS-ZEdYDAQ787o2OY9n7sYgHa8F6X4VhlXM_7_0adzbCJoj9TZPcANlPQ"
  const eventId = "846e4e3c-a215-4981-b672-d0ef1f3174a9"
  let email = `${randomString(10)}@foo`
  const params = {
    headers: {
      Authorization: `bearer ${token}`,
      "Content-Type": "application/json"
    }
  }
  const payload = {
    name: randomString(10),
    email: {
      email
    },
    participantAnswers: [],
    cancelUrlTemplate: "http://localhost:3000/events/{eventId}/cancel/{email}?cancellationToken=%7BcancellationToken%7D",
  };
  const url = `http://localhost:5000/api/events/${eventId}/participants/${email}`
  let response = http.post(url, JSON.stringify(payload), params);

  check(response, {
    'has 200 status': (r) => r.status === 200
  })
};

export function handleSummary(data) {
  return {
    "Spike-RegisterParticpants.html": htmlReport(data),
  };
}
